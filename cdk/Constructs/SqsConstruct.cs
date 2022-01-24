using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace Aws.Otel.Cdk.Stack.Constructs
{
    public class SqsConstruct: Construct
    {
        public SqsConstruct(Construct scope, string id, User user) 
            : base(scope, id)
        {
            var queue = new Queue(this, 
                "aws-otel-demo-sqs-queue",
                new QueueProps
            {
                QueueName = "aws-otel-demo-sqs-queue",
                VisibilityTimeout = Duration.Seconds(30),
            });

            var policy = new PolicyStatement(new PolicyStatementProps
            {
                Actions = new[] { "sqs:*" },
                Resources = new[] {queue.QueueArn },
                Principals = new IPrincipal[] { user }
            });

            queue.AddToResourcePolicy(policy);

            _ = new CfnOutput(this, 
                "sqs-uri", 
                new CfnOutputProps
            {
                ExportName = "sqs-queue-uri",
                Description = "SQS Queue URI ",
                Value = queue.QueueUrl
            });
        }
    }
}
