using Amazon.CDK;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace Aws.Otel.Cdk.Stack.Constructs
{
    public class SqsConstruct: Construct
    {
        public SqsConstruct(Construct scope, string id) 
            : base(scope, id)
        {
            _ = new Queue(this, "lambda-queue", new QueueProps
            {
                QueueName = "aws-otel-demo-sqs-queue",
                VisibilityTimeout = Duration.Seconds(30),
            });
        }
    }
}
