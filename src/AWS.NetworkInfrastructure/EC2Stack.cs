using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace AWS.NetworkInfrastructure;

/// <summary>
/// Represents a CDK Stack that creates a bastion host and a private EC2 instance within an existing VPC.
/// </summary>
/// <remarks>
/// This stack demonstrates the following capabilities:
/// <list type="bullet">
/// <item>
///     <description>Importing VPC, public subnet, and private subnet information from another stack using <see cref="Fn.ImportValue"/>.</description>
/// </item>
/// <item>
///     <description>Creating a key pair for SSH access to EC2 instances.</description>
/// </item>
/// <item>
///     <description>Creating two security groups:
///         <list type="bullet">
///             <item><description>Bastion host security group allowing SSH access (port 22) from any IPv4 address.</description></item>
///             <item><description>Private instance security group allowing SSH access only from the bastion host.</description></item>
///         </list>
///     </description>
/// </item>
/// <item>
///     <description>Launching two EC2 instances:
///         <list type="bullet">
///             <item><description>A bastion host in a public subnet.</description></item>
///             <item><description>A private instance in a private subnet.</description></item>
///             <item><description>Both using the latest Amazon Linux 2 AMI and t2.micro instance type.</description></item>
///             <item><description>Each associated with its respective security group.</description></item>
///             <item><description>Bastion host assigned a public IP, private instance with only a private IP.</description></item>
///         </list>
///     </description>
/// </item>
/// <item>
///     <description>Outputting the public IP address of the bastion host and the private IP address of the private instance.</description>
/// </item>
/// </list>
/// 
/// This stack depends on a previously created VPC stack that exports the following values:
/// <list type="bullet">
/// <item><description>VPC ID with export name: {StackName}-VpcId</description></item>
/// <item><description>Public Subnet IDs with export name: {StackName}-PublicSubnetIds</description></item>
/// <item><description>Private Subnet IDs with export name: {StackName}-PrivateSubnetIds</description></item>
/// </list>
/// 
/// Note: This stack uses low-level L1 constructs (Cfn*) for resources to provide more
/// direct control over the created AWS resources and to demonstrate a bastion host architecture for enhanced security.
/// </remarks>

public class EC2Stack : Stack
{
    public EC2Stack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
        // Import VPC ID
        var vpcId = Fn.ImportValue($"{props.StackName.Replace("EC2", "VPC")}-VpcId");
        
        // Import Public Subnet IDs
        var publicSubnetIdsString = Fn.ImportValue($"{props.StackName.Replace("EC2", "VPC")}-PublicSubnetIds");
        var privateSubnetIdsString = Fn.ImportValue($"{props.StackName.Replace("EC2", "VPC")}-PrivateSubnetIds");
        
        // Create a new key pair
        var keyPair = new CfnKeyPair(this, "DemoKeyPair", new CfnKeyPairProps
        {
            KeyName = "demo-key-pair"
        });
        
        // Create a security group for the Bastion host
        var bastionSecurityGroup = new CfnSecurityGroup(this, "BastionSecurityGroup", new CfnSecurityGroupProps
        {
            GroupName = "BastionSecurityGroup",
            GroupDescription = "Security group for Bastion host",
            VpcId = vpcId,
            SecurityGroupIngress = new[]
            {
                new CfnSecurityGroup.IngressProperty
                {
                    IpProtocol = "tcp",
                    FromPort = 22,
                    ToPort = 22,
                    CidrIp = "0.0.0.0/0"
                }
            }
        });
        
        // Create a security group for the private EC2 instance
        var privateInstanceSecurityGroup = new CfnSecurityGroup(this, "PrivateInstanceSecurityGroup", new CfnSecurityGroupProps
        {
            GroupName = "PrivateInstanceSecurityGroup",
            GroupDescription = "Security group for private EC2 instance",
            VpcId = vpcId,
            SecurityGroupIngress = new[]
            {
                new CfnSecurityGroup.IngressProperty
                {
                    IpProtocol = "tcp",
                    FromPort = 22,
                    ToPort = 22,
                    SourceSecurityGroupId = bastionSecurityGroup.Ref
                }
            }
        });
        
        // Get the latest Amazon Linux 2 AMI
        var ami = new AmazonLinuxImage(new AmazonLinuxImageProps
        {
            Generation = AmazonLinuxGeneration.AMAZON_LINUX_2,
            CpuType = AmazonLinuxCpuType.X86_64
        });
        
        // Create the Bastion host EC2 instance
        var bastionInstance = new CfnInstance(this, "BastionHost", new CfnInstanceProps
        {
            InstanceType = InstanceType.Of(InstanceClass.T2, InstanceSize.MICRO).ToString(),
            ImageId = ami.GetImage(this).ImageId,
            KeyName = keyPair.KeyName,
            NetworkInterfaces = new[]
            {
                new CfnInstance.NetworkInterfaceProperty
                {
                    DeviceIndex = "0",
                    AssociatePublicIpAddress = true,
                    DeleteOnTermination = true,
                    SubnetId = Fn.Select(0, Fn.Split(",", publicSubnetIdsString)),
                    GroupSet = new[] { bastionSecurityGroup.Ref }
                }
            },
            Tags = new[]
            {
                new CfnTag { Key = "Name", Value = "BastionHost" }
            }
        });

        // Create the private EC2 instance
        var privateInstance = new CfnInstance(this, "PrivateEC2Instance", new CfnInstanceProps
        {
            InstanceType = InstanceType.Of(InstanceClass.T2, InstanceSize.MICRO).ToString(),
            ImageId = ami.GetImage(this).ImageId,
            KeyName = keyPair.KeyName,
            NetworkInterfaces = new[]
            {
                new CfnInstance.NetworkInterfaceProperty
                {
                    DeviceIndex = "0",
                    AssociatePublicIpAddress = false,
                    DeleteOnTermination = true,
                    SubnetId = Fn.Select(0, Fn.Split(",", privateSubnetIdsString)),
                    GroupSet = new[] { privateInstanceSecurityGroup.Ref }
                }
            },
            Tags = new[]
            {
                new CfnTag { Key = "Name", Value = "PrivateEC2Instance" }
            }
        });

        // Output the public IP of the Bastion host
        new CfnOutput(this, "BastionPublicIP", new CfnOutputProps
        {
            Description = "Public IP address of the Bastion host",
            Value = Fn.GetAtt(bastionInstance.LogicalId, "PublicIp").ToString()
        });

        // Output the private IP of the private EC2 instance
        new CfnOutput(this, "PrivateInstancePrivateIP", new CfnOutputProps
        {
            Description = "Private IP address of the private EC2 instance",
            Value = Fn.GetAtt(privateInstance.LogicalId, "PrivateIp").ToString()
        });
    }
}