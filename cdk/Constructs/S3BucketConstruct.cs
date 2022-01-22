using System;
using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace Aws.Otel.Cdk.Stack.Constructs
{
    public class S3BucketConstruct : Construct
    {
        public S3BucketConstruct(Construct scope, string id, User user) 
            : base(scope, id)
        {

            var bucket = new Bucket(this, 
                "aws-otel-demo-s3-bucket", 
                new BucketProps
            {
                BucketName = $"aws-otel-demo-s3-bucket-{new Random().Next(1,50000)}",
                RemovalPolicy = RemovalPolicy.DESTROY
            });

            var policy = new PolicyStatement(new PolicyStatementProps
            {
                Actions = new []{"s3:*"},
                Resources = new []{ bucket.ArnForObjects("*") },
                Principals = new IPrincipal[]{ user }
            });

            bucket.AddToResourcePolicy(policy);

            _ = new CfnOutput(this, "s3-bucket-name", new CfnOutputProps
            {
                Description = "S3 Bucket Name",
                Value = bucket.BucketName
            });

        }
    }
}
