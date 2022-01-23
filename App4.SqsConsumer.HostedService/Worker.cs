using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.SQS;
using Amazon.SQS.Model;
using App4.SqsConsumer.HostedService.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;

namespace App4.SqsConsumer.HostedService
{
    public class Worker : BackgroundService
    {
        private static readonly ActivitySource Activity = new(nameof(Worker));
        private static readonly TextMapPropagator Propagator = new AWSXRayPropagator();

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
                        AttributeNames = new List<string> { "All" },
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

            var parentContext = Propagator.Extract(default,
                    msg,
                    ActivityHelper.ExtractTraceContextFromMessage);

            Baggage.Current = parentContext.Baggage;

            using (var activity = Activity.StartActivity("Process SQS Message", 
                ActivityKind.Server, 
                parentContext.ActivityContext))
            {
                ActivityHelper.AddActivityTags(activity);
                
                _logger.LogInformation("Add item to DynamoDb");

                var items = Table.LoadTable(_dynamoDb, "Items");
                
                var doc = new Document
                {
                    ["Id"] = Guid.NewGuid(),
                    ["Message"] = msg.Body
                };

                await items.PutItemAsync(doc, cancellationToken);
                await _sqs.DeleteMessageAsync(_configuration["SQS:URI"], msg.ReceiptHandle, cancellationToken);
                
            }
        }

     
    }
}
