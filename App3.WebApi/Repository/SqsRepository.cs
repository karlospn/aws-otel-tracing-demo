﻿using System.Diagnostics;
using Amazon.SQS;
using Amazon.SQS.Model;
using App3.WebApi.Events;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;
using System;
using System.Collections.Generic;

namespace App3.WebApi.Repository
{
    public class SqsRepository: ISqsRepository
    {
        private static readonly ActivitySource Activity = new(nameof(SqsRepository));
        private static readonly TextMapPropagator Propagator = new AWSXRayPropagator();

        private readonly IAmazonSQS _client;
        private readonly IConfiguration _configuration;

        public SqsRepository(IAmazonSQS client, 
            IConfiguration configuration)
        {
            _client = client;
            _configuration = configuration;
        }

        public async Task Publish(IEvent evt)
        {
            using (var activity = Activity.StartActivity("SQS Publish", ActivityKind.Producer))
            {

                var traceAttributeValue = new Dictionary<string, MessageSystemAttributeValue>();
                AddActivityToHeader(activity, traceAttributeValue);

                await _client.SendMessageAsync(new SendMessageRequest
                {
                    MessageBody = (evt as MessagePersistedEvent)?.Message,
                    QueueUrl = _configuration["Sqs:Uri"],
                    MessageSystemAttributes = traceAttributeValue,
                    
                });
            }
        }

        private void AddActivityToHeader(Activity activity, Dictionary<string, MessageSystemAttributeValue> props)
        {
            Propagator.Inject(new PropagationContext(activity.Context, Baggage.Current), props, InjectContextIntoHeader);
            activity?.SetTag("messaging.system", "sqs");

        }

        private void InjectContextIntoHeader(Dictionary<string, MessageSystemAttributeValue> props, string key, string value)
        {
            props["AWSTraceHeader"] = new MessageSystemAttributeValue
            {
                StringValue = value,
                DataType = "String"
            };
        }
    }
}
