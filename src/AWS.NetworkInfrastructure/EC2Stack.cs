using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Constructs;

namespace AWS.NetworkInfrastructure;

/// <summary>
/// Represents a CDK Stack that creates an EC2 instance within an existing VPC.
/// </summary>
/// <remarks>
/// This stack demonstrates the following capabilities:
/// <list type="bullet">
/// <item>
///     <description>Importing VPC and subnet information from another stack using <see cref="Fn.ImportValue"/>.</description>
/// </item>
/// <item>
///     <description>Creating a security group with SSH access (port 22) allowed from any IPv4 address.</description>
/// </item>
/// <item>
///     <description>Launching an EC2 instance with the following specifications:
///         <list type="bullet">
///             <item><description>Uses the latest Amazon Linux 2 AMI.</description></item>
///             <item><description>Instance type is t2.micro.</description></item>
///             <item><description>Placed in the first public subnet of the imported VPC.</description></item>
///             <item><description>Associated with the created security group.</description></item>
///             <item><description>Assigned a public IP address.</description></item>
///         </list>
///     </description>
/// </item>
/// <item>
///     <description>Outputting the public IP address of the created EC2 instance.</description>
/// </item>
/// </list>
/// 
/// This stack depends on a previously created VPC stack that exports the following values:
/// <list type="bullet">
/// <item><description>VPC ID with export name: {StackName}-VpcId</description></item>
/// <item><description>Public Subnet IDs with export name: {StackName}-PublicSubnetIds</description></item>
/// </list>
/// 
/// Note: This stack uses low-level L1 constructs (Cfn*) for some resources to provide more
/// direct control over the created AWS resources.
/// </remarks>
public class EC2Stack : Stack
{
    public EC2Stack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
        // Import VPC ID
        var vpcId = Fn.ImportValue($"{props.StackName.Replace("EC2", "VPC")}-VpcId");
        
        // Import Public Subnet IDs
        var publicSubnetIdsString = Fn.ImportValue($"{props.StackName.Replace("EC2", "VPC")}-PublicSubnetIds");
        
        // Create a security group for the EC2 instance
        var securityGroup = new CfnSecurityGroup(this, "EC2SecurityGroup", new CfnSecurityGroupProps
        {
            GroupDescription = "Security group for EC2 instance",
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
        
        // Get the latest Amazon Linux 2 AMI
        var ami = new AmazonLinuxImage(new AmazonLinuxImageProps
        {
            Generation = AmazonLinuxGeneration.AMAZON_LINUX_2,
            CpuType = AmazonLinuxCpuType.X86_64
        });

        // Create an EC2 instance
        var ec2Instance = new CfnInstance(this, "MyEC2Instance", new CfnInstanceProps
        {
            InstanceType = InstanceType.Of(InstanceClass.T2, InstanceSize.MICRO).ToString(),
            ImageId = ami.GetImage(this).ImageId,
            NetworkInterfaces = new[]
            {
                new CfnInstance.NetworkInterfaceProperty
                {
                    DeviceIndex = "0",
                    AssociatePublicIpAddress = true,
                    DeleteOnTermination = true,
                    SubnetId = Fn.Select(0, Fn.Split(",", publicSubnetIdsString)),
                    GroupSet = new[] { securityGroup.Ref }
                }
            },
            Tags = new[]
            {
                new CfnTag { Key = "Name", Value = "MyEC2Instance" }
            }
        });

        // Output the public IP of the EC2 instance
        new CfnOutput(this, "EC2PublicIP", new CfnOutputProps
        {
            Description = "Public IP address of the EC2 instance",
            Value = Fn.GetAtt(ec2Instance.LogicalId, "PublicIp").ToString()
        });
    }
}