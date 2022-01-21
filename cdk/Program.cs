using Amazon.CDK;
using Aws.Otel.Cdk.Stack.Stacks;

namespace Aws.Otel.Cdk.Stack
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            
            new CdkStack(app, "AwsOtelDemoStack", new StackProps
            {
                Env = new Amazon.CDK.Environment
                {
                    Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                    Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION"),
                }
            });

            app.Synth();
        }
    }
}
