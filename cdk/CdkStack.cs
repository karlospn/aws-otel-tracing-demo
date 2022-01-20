using Amazon.CDK;
using Constructs;

namespace Aws.Otel.Cdk.Stack
{
    public class CdkStack : Amazon.CDK.Stack
    {
        internal CdkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // The code that defines your stack goes here
        }
    }
}
