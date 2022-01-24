using System.Linq;
using Amazon.CDK;
using Amazon.CDK.AWS.AmazonMQ;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace Aws.Otel.Cdk.Stack.Constructs
{
    internal class ActiveMqRabbitConstruct : Construct
    {
        public ActiveMqRabbitConstruct(Construct scope, string id, Vpc vpc) 
            : base(scope, id)
        {
            var subnetIds = vpc.PublicSubnets
                .Select(x => x.SubnetId)
                .ToArray();

            var cluster = new CfnBroker(this, 
                "aws-otel-tracing-demo-rabbit-cluster", 
                new CfnBrokerProps
            {
                BrokerName = "aws-otel-tracing-demo-rabbit-cluster",
                EngineType = "RABBITMQ",
                PubliclyAccessible = true,
                Users = new[] { new CfnBroker.UserProperty {
                    Username = "specialguest",
                    Password = "P@ssw0rd111!",
                    ConsoleAccess = true,
                    Groups = new [] { "administrator" }
                }},
                AutoMinorVersionUpgrade = false,
                DeploymentMode = "SINGLE_INSTANCE",
                EngineVersion = "3.8.26",
                HostInstanceType = "mq.t3.micro",
                SubnetIds = subnetIds
            });

            _ = new CfnOutput(this,
                "rabbit-endpoint",
                new CfnOutputProps
                { 
                    ExportName = "rabbitmq-host-endpoint",
                    Description = "Rabbit Endpoint",
                    Value = Fn.Select(0, cluster.AttrAmqpEndpoints)

                });

        }
    }
}
