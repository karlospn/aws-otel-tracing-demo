using Amazon.CDK;
using Aws.Otel.Cdk.Stack.Constructs;
using Constructs;

namespace Aws.Otel.Cdk.Stack.Stacks
{
    public class CdkStack : Amazon.CDK.Stack
    {
        internal CdkStack(Construct scope, 
            string id, 
            IStackProps props = null) 
            : base(scope, id, props)
        {
            _ = new IamUserConstruct(this,
                "aws-otel-demo-user");

            _ = new ActiveMqRabbitConstruct(this,
                "aws-otel-demo-rabbit-cluster");

            _ = new S3BucketConstruct(this,
                "aws-otel-demo-s3-bucket");

            _ = new SqsConstruct(this,
                "aws-otel-demo-sqs-queue");

            _ = new ElasticCacheRedisConstruct(this,
                "aws-otel-demo-redis-cache");
        }
    }
}
