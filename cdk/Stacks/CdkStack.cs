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
            var vpc = new VpcConstruct(this,
                "aws-otel-demo-vpc-construct");

            var iamUser = new IamUserConstruct(this,
                "aws-otel-demo-iam-user-construct");

            _ = new DynamoDbTableConstruct(this,
                "aws-otel-demo-redis-cache-construct");

            _ = new S3BucketConstruct(this,
                "aws-otel-demo-s3-bucket-construct", iamUser.User);

            _ = new SqsConstruct(this,
                "aws-otel-demo-sqs-queue-construct", iamUser.User);

            _ = new ActiveMqRabbitConstruct(this,
                "aws-otel-demo-rabbit-cluster-construct", vpc.Vpc);


        }
    }
}
