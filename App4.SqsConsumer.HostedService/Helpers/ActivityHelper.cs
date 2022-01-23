using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Amazon.SQS.Model;

namespace App4.SqsConsumer.HostedService.Helpers
{
    public static class ActivityHelper
    {
        public static IEnumerable<string> ExtractTraceContextFromMessage(Message msg, string key)
        {
            try
            {
                if (msg.Attributes.TryGetValue("AWSTraceHeader", out var value))
                {
                    return new[] { value };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to extract trace context: {ex}");
            }

            return Enumerable.Empty<string>();
        }

        public static void AddActivityTags(Activity activity)
        {
            activity?.SetTag("messaging.system", "sqs");
        }
    }
}
