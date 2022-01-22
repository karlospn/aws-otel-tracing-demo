using Amazon.CDK.AWS.EC2;
using Constructs;

namespace Aws.Otel.Cdk.Stack.Constructs
{
    public class VpcConstruct : Construct
    {
        public Vpc Vpc { get; set; }
        public VpcConstruct(Construct scope, string id) 
            : base(scope, id)
        {
            Vpc = new Vpc(this, 
                "aws-otel-demo-vpc", 
                new VpcProps
            {
                Cidr = "10.30.0.0/16",
                MaxAzs = 1,
                NatGateways = 0,
                VpcName = "aws-otel-demo-vpc",
                SubnetConfiguration = new []
                {
                    new SubnetConfiguration
                    {
                        Name = "aws-otel-demo-public-subnet",
                        CidrMask = 24,
                        SubnetType = SubnetType.PUBLIC
                    }
                }
            });
        }

    }
}
