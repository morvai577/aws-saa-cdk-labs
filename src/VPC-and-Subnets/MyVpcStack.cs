using Amazon.CDK;
using Amazon.CDK.AWS.EC2;

namespace VPC_and_Subnets;

public sealed class MyVpcStack : Stack
{
    public MyVpcStack(Constructs.Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
        // Create a new VPC
        var vpc = new Vpc(this, "DemoVPC", new VpcProps
        {
            VpcName = "DemoVPC",
            AvailabilityZones = ["us-east-1a", "us-east-1b"],
            IpAddresses = IpAddresses.Cidr("10.0.0.0/16"),
            NatGateways = 0,
            SubnetConfiguration = new SubnetConfiguration[]
            {
                new SubnetConfiguration
                {
                    CidrMask = 24,
                    Name = "Public",
                    SubnetType = SubnetType.PUBLIC,
                }
            },
            EnableDnsHostnames = false,
            EnableDnsSupport = true,
        });
    }
}