using Amazon.CDK;
using Amazon.CDK.AWS.EC2;

namespace AWS.NetworkInfrastructure;

/// <summary>
/// Represents a CDK Stack that creates a custom VPC with public and private subnets.
/// </summary>
/// <remarks>
/// This stack demonstrates the following capabilities:
/// <list type="bullet">
/// <item>
///     <description>Creating a custom VPC with specified CIDR block.</description>
/// </item>
/// <item>
///     <description>Creating an Internet Gateway and attaching it to the VPC.</description>
/// </item>
/// <item>
///     <description>Creating two public and two private subnets across two availability zones.</description>
/// </item>
/// <item>
///     <description>Configuring public subnets to auto-assign public IP addresses.</description>
/// </item>
/// <item>
///     <description>Creating separate route tables for public and private subnets.</description>
/// </item>
/// <item>
///     <description>Adding a route to the Internet Gateway in the public route table.</description>
/// </item>
/// <item>
///     <description>Associating subnets with their respective route tables.</description>
/// </item>
/// <item>
///     <description>Exporting VPC ID and Public Subnet IDs for use in other stacks.</description>
/// </item>
/// </list>
/// 
/// This stack exports the following values:
/// <list type="bullet">
/// <item><description>VPC ID with export name: {StackName}-VpcId</description></item>
/// <item><description>Public Subnet IDs with export name: {StackName}-PublicSubnetIds</description></item>
/// </list>
/// 
/// Note: This stack uses low-level L1 constructs (Cfn*) to provide more
/// direct control over the created AWS resources and to demonstrate manual
/// creation of VPC components.
/// </remarks>
public sealed class MyVpcStack : Stack
{
    public MyVpcStack(Constructs.Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
        // Create a VPC manually
        var cfnVpc = new CfnVPC(this, "DemoVPC", new CfnVPCProps
        {
            CidrBlock = "10.0.0.0/16",
            EnableDnsHostnames = true,
            EnableDnsSupport = true,
            InstanceTenancy = "default",
            Tags = new[] { new CfnTag { Key = "Name", Value = "DemoVPC" } }
        });

        // Create an Internet Gateway
        var cfnInternetGateway = new CfnInternetGateway(this, "DemoIGW", new CfnInternetGatewayProps
        {
            Tags = new[] { new CfnTag { Key = "Name", Value = "DemoIGW" } }
        });

        // Attach the Internet Gateway to the VPC
        new CfnVPCGatewayAttachment(this, "IGWAttachment", new CfnVPCGatewayAttachmentProps
        {
            VpcId = cfnVpc.Ref,
            InternetGatewayId = cfnInternetGateway.Ref
        });

        // Create public subnets
        var publicSubnetA = new CfnSubnet(this, "PublicSubnetA", new CfnSubnetProps
        {
            VpcId = cfnVpc.Ref,
            AvailabilityZone = $"{this.Region}a",
            CidrBlock = "10.0.1.0/24",
            MapPublicIpOnLaunch = true,
            Tags = new[] { new CfnTag { Key = "Name", Value = "Public Subnet A" } }
        });

        var publicSubnetB = new CfnSubnet(this, "PublicSubnetB", new CfnSubnetProps
        {
            VpcId = cfnVpc.Ref,
            AvailabilityZone = $"{this.Region}b",
            CidrBlock = "10.0.2.0/24",
            MapPublicIpOnLaunch = true,
            Tags = new[] { new CfnTag { Key = "Name", Value = "Public Subnet B" } }
        });

        // Create private subnets
        var privateSubnetA = new CfnSubnet(this, "PrivateSubnetA", new CfnSubnetProps
        {
            VpcId = cfnVpc.Ref,
            AvailabilityZone = $"{this.Region}a",
            CidrBlock = "10.0.3.0/24",
            MapPublicIpOnLaunch = false,
            Tags = new[] { new CfnTag { Key = "Name", Value = "Private Subnet A" } }
        });

        var privateSubnetB = new CfnSubnet(this, "PrivateSubnetB", new CfnSubnetProps
        {
            VpcId = cfnVpc.Ref,
            AvailabilityZone = $"{this.Region}b",
            CidrBlock = "10.0.4.0/24",
            MapPublicIpOnLaunch = false,
            Tags = new[] { new CfnTag { Key = "Name", Value = "Private Subnet B" } }
        });

        // Create public route table
        var publicRouteTable = new CfnRouteTable(this, "PublicRouteTable", new CfnRouteTableProps
        {
            VpcId = cfnVpc.Ref,
            Tags = new[] { new CfnTag { Key = "Name", Value = "Public Route Table" } }
        });

        // Create private route table
        var privateRouteTable = new CfnRouteTable(this, "PrivateRouteTable", new CfnRouteTableProps
        {
            VpcId = cfnVpc.Ref,
            Tags = new[] { new CfnTag { Key = "Name", Value = "Private Route Table" } }
        });

        // Create public route
        new CfnRoute(this, "PublicRoute", new CfnRouteProps
        {
            RouteTableId = publicRouteTable.Ref,
            DestinationCidrBlock = "0.0.0.0/0",
            GatewayId = cfnInternetGateway.Ref
        });

        // Associate public subnets with public route table
        new CfnSubnetRouteTableAssociation(this, "PublicSubnetARouteTableAssociation", new CfnSubnetRouteTableAssociationProps
        {
            SubnetId = publicSubnetA.Ref,
            RouteTableId = publicRouteTable.Ref
        });

        new CfnSubnetRouteTableAssociation(this, "PublicSubnetBRouteTableAssociation", new CfnSubnetRouteTableAssociationProps
        {
            SubnetId = publicSubnetB.Ref,
            RouteTableId = publicRouteTable.Ref
        });

        // Associate private subnets with private route table
        new CfnSubnetRouteTableAssociation(this, "PrivateSubnetARouteTableAssociation", new CfnSubnetRouteTableAssociationProps
        {
            SubnetId = privateSubnetA.Ref,
            RouteTableId = privateRouteTable.Ref
        });

        new CfnSubnetRouteTableAssociation(this, "PrivateSubnetBRouteTableAssociation", new CfnSubnetRouteTableAssociationProps
        {
            SubnetId = privateSubnetB.Ref,
            RouteTableId = privateRouteTable.Ref
        });
        
        // Export VPC ID
        new CfnOutput(this, "VpcId", new CfnOutputProps
        {
            Value = cfnVpc.Ref,
            ExportName = $"{this.StackName}-VpcId"
        });

        // Export Public Subnet IDs
        new CfnOutput(this, "PublicSubnetIds", new CfnOutputProps
        {
            Value = $"{publicSubnetA.Ref},{publicSubnetB.Ref}",
            ExportName = $"{this.StackName}-PublicSubnetIds"
        });
        
        // Export Private Subnet IDs
        new CfnOutput(this, "PrivateSubnetIds", new CfnOutputProps
        {
            Value = $"{privateSubnetA.Ref},{privateSubnetB.Ref}",
            ExportName = $"{this.StackName}-PrivateSubnetIds"
        });
    }
}