using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using App4.SqsConsumer.HostedService.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using Status = OpenTelemetry.Trace.Status;

namespace App4.SqsConsumer.HostedService
{
    public class Worker : BackgroundService
    {
        private static readonly ActivitySource Activity = new(nameof(Worker));
        private static readonly ActivitySource ActivityDynamo = new("Dynamo.PutItem");
        private static readonly ActivitySource ActivitySQSDelete = new("SQS.DeleteMessage");

        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAmazonSQS _sqs;
        private readonly IAmazonDynamoDB _dynamoDb;


        public Worker(ILogger<Worker> logger,
            IConfiguration configuration, 
            IAmazonSQS sqs, 
            IAmazonDynamoDB dynamoDb)
        {
            _logger = logger;
            _configuration = configuration;
            _sqs = sqs;
            _dynamoDb = dynamoDb;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var request = new ReceiveMessageRequest
                    {
                        QueueUrl = _configuration["SQS:URI"],
                        MaxNumberOfMessages = 1,
                        WaitTimeSeconds = 5,
                        AttributeNames = new List<string> { "All" }
                    };

                    var result = await _sqs.ReceiveMessageAsync(request, stoppingToken);

                    if (result.Messages.Any())
                    {
                        await ProcessMessage(result.Messages[0], stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                }

                _logger.LogInformation("Worker running at: {time}", DateTime.UtcNow);
                Thread.Sleep(10000);
            }
        }

        private async Task ProcessMessage(Message msg, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing messages from SQS");

            var propagator = Propagators.DefaultTextMapPropagator;

            var parentContext = propagator.Extract(default,
                    msg,
                    ActivityHelper.ExtractTraceContextFromMessage);

            Baggage.Current = parentContext.Baggage;

            using (var activity = Activity.StartActivity("Process SQS Message", 
                ActivityKind.Server, 
                parentContext.ActivityContext))
            {
                using (var dynamoActivity = Activity.StartActivity("Dynamo.PutItem",
                           ActivityKind.Client,
                           activity.Context))
                {
                    try
                    {
                        _logger.LogInformation("Add item to DynamoDb");

                        dynamoActivity?.SetTag("aws.service", "DynamoDbV2");
                        dynamoActivity?.SetTag("aws.operation", "PutItem");
                        dynamoActivity?.SetTag("aws.table_name", "Items");
                        dynamoActivity?.SetTag("aws.region", "eu-west-1");

                        var items = Table.LoadTable(_dynamoDb, "Items");

                        var doc = new Document
                        {
                            ["Id"] = Guid.NewGuid(),
                            ["Message"] = msg.Body
                        };

                        await items.PutItemAsync(doc, cancellationToken);

                    }
                    catch (Exception ex)
                    {
                        if (dynamoActivity != null)
                        {
                            ProcessException(dynamoActivity, ex);
                        }
                        throw;
                    }
                }

                using (var sqsActivity = Activity.StartActivity("SQS.DeleteMessage",
                           ActivityKind.Client,
                           activity.Context))
                {
                    try
                    {
                        sqsActivity?.SetTag("aws.service", "SQS");
                        sqsActivity?.SetTag("aws.operation", "DeleteMessage");
                        sqsActivity?.SetTag("aws.queue_url", _configuration["SQS:URI"]);
                        sqsActivity?.SetTag("aws.region", "eu-west-1");
                        var response = await _sqs.DeleteMessageAsync(_configuration["SQS:URI"], msg.ReceiptHandle, cancellationToken);
                        sqsActivity?.SetTag("aws.requestId", response.ResponseMetadata.RequestId);
                        sqsActivity?.SetTag("http.status_code", (int)response.HttpStatusCode);
                        

                    }
                    catch (Exception ex)
                    {
                        if (sqsActivity != null)
                        {
                            ProcessException(sqsActivity, ex);
                        }
                        throw;
                    }
                }
            }
        }

        private void ProcessException(Activity activity, Exception exception)
        {
            activity.RecordException(exception);
            activity.SetStatus(Status.Error.WithDescription(exception.Message));
        }
    }
}
