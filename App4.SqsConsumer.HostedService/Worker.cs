using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using App4.SqsConsumer.HostedService.Helpers;
using Microsoft.Extensions.Caching.Distributed;
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
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;
        private readonly IAmazonSQS _sqs;


        public Worker(ILogger<Worker> logger,
            IDistributedCache cache,
            IConfiguration configuration, 
            IAmazonSQS sqs)
        {
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
            _sqs = sqs;
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
                        AttributeNames = new List<string> { "All"}, 
                    };

                    var result = await _sqs.ReceiveMessageAsync(request.QueueUrl, stoppingToken);

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
                
                var item = await _cache.GetStringAsync("sqs.msg", cancellationToken);

                if (string.IsNullOrEmpty(item))
                {
                    _logger.LogInformation("Add item into redis cache");

                    await _cache.SetStringAsync("sqs.msg",
                        msg.Body,
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(1)
                        },
                        cancellationToken);

                    await _sqs.DeleteMessageAsync(_configuration["SQS:URI"], msg.ReceiptHandle, cancellationToken);
                }
            }
        }

     
    }
}
