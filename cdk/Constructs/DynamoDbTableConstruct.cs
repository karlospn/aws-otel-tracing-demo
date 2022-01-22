using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;

namespace Aws.Otel.Cdk.Stack.Constructs
{
    public class DynamoDbTableConstruct : Construct
    {
        public DynamoDbTableConstruct(Construct scope, string id) 
            : base(scope, id)
        {
            _ = new Table(this,
                "aws-otel-tracing-demo-items-table",
                new TableProps
                {
                    TableName = "items",
                    RemovalPolicy = RemovalPolicy.DESTROY,
                    PartitionKey = new Attribute { Name = "Id", Type = AttributeType.STRING },
                    ReadCapacity = 5,
                    WriteCapacity = 5,
                    BillingMode = BillingMode.PROVISIONED
                });
        }
    }
}
