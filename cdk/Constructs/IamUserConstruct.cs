using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Constructs;

namespace Aws.Otel.Cdk.Stack.Constructs
{
    public class IamUserConstruct :Construct
    {
        public User User { get; set; }
        public IamUserConstruct(Construct scope, string id) 
            : base(scope, id)
        {

            User = new User(this,
                "iam-user",
                new UserProps
            {
                UserName = "aws-otel-tracing-demo-user",
                ManagedPolicies = new []
                {
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonS3FullAccess"),
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonSQSFullAccess"),
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonDynamoDBFullAccess")
                }
            });

            var collectorPolicy = new PolicyStatement(new PolicyStatementProps
            {
                Actions = new[]
                {
                    "logs:PutLogEvents",
                    "logs:CreateLogGroup",
                    "logs:CreateLogStream",
                    "logs:DescribeLogStreams",
                    "logs:DescribeLogGroups",
                    "xray:PutTraceSegments",
                    "xray:PutTelemetryRecords",
                    "xray:GetSamplingRules",
                    "xray:GetSamplingTargets",
                    "xray:GetSamplingStatisticSummaries",
                    "ssm:GetParameters"
                },
                Resources = new[] { "*" },
                Effect = Effect.ALLOW
            });

            User.AddToPolicy(collectorPolicy);
            

            var accessKey = new AccessKey(this, 
                "iam-user-access-key", 
                new AccessKeyProps
            {
                User = User
            });

            _ = new CfnOutput(this, 
                "user-key", 
                new CfnOutputProps
            {
                Description = "IAM User access key",
                Value = accessKey.AccessKeyId
            });

            _ = new CfnOutput(this, 
                "user-secret", 
                new CfnOutputProps
            {
                Description = "IAM User secret key",
                Value = accessKey.SecretAccessKey.ToString()
            });

        }
    }
}
