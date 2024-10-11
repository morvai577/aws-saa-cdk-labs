using Amazon.CDK;
using VPC_and_Subnets;

// Create the CDK app
var app = new App();

// Create the VPC stack
new MyVpcStack(app, "MyVpcStack", new StackProps
{
    // If you don't specify 'env', this stack will be environment-agnostic.
    // Account/Region-dependent features and context lookups will not work,
    // but a single synthesized template can be deployed anywhere.

    // Uncomment the next block to specialize this stack for the AWS Account
    // and Region that are implied by the current CLI configuration.
    
    /*
    Env = new Amazon.CDK.Environment
    {
        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
        Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION"),
    }
    */

    // Uncomment the next block if you know exactly what Account and Region you
    // want to deploy the stack to.
    
    Env = new Amazon.CDK.Environment
    {
        Account = "381492186126",
        Region = "us-east-1",
    },
    
    // Add custom synthesis options
    Synthesizer = new DefaultStackSynthesizer(new DefaultStackSynthesizerProps
    {
        GenerateBootstrapVersionRule = false
    })
});

Tags.Of(app).Add("Name", "DemoVPC");

// Synthesize the CloudFormation template
app.Synth();