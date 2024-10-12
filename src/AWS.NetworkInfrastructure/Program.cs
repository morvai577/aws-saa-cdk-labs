using Amazon.CDK;
using AWS.NetworkInfrastructure;

var app = new App();

var vpcStack = new MyVpcStack(app, "MyVpcStack", new StackProps
{
    // If you don't specify 'env', this stack will be environment-agnostic.
    // Account/Region-dependent features and context lookups will not work,
    // but a single synthesized template can be deployed anywhere.

    // Uncomment the next block to specialize this stack for the AWS Account
    // and Region that are implied by the current CLI configuration.
    Env = new Amazon.CDK.Environment
    {
        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
        Region = "us-east-1"
    },
    
    // Add custom synthesis options
    Synthesizer = new DefaultStackSynthesizer(new DefaultStackSynthesizerProps
    {
        GenerateBootstrapVersionRule = false
    })
});

var ec2Stack = new EC2Stack(app, "MyEC2Stack", new StackProps
{
    // Ensure the EC2 stack is created after the VPC stack
    Env = new Amazon.CDK.Environment
    {
        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
        Region = "us-east-1"
    },
    StackName = vpcStack.StackName.Replace("VPC", "EC2"),
    // Add custom synthesis options
    Synthesizer = new DefaultStackSynthesizer(new DefaultStackSynthesizerProps
    {
        GenerateBootstrapVersionRule = false
    })
});

// Add a dependency to ensure the EC2 stack is created after the VPC stack
ec2Stack.AddDependency(vpcStack);

app.Synth();