using Amazon.CDK.AWS.EC2;
using Constructs;

namespace Aws.Otel.Cdk.Stack.Constructs
{
    internal class ElasticCacheRedisConstruct : Construct
    {
        public ElasticCacheRedisConstruct(Construct scope, string id, Vpc vpc) 
            : base(scope, id)
        {

        }
    }
}
