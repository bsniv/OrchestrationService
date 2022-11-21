using FluentAssertions;
using Microsoft.Extensions.Logging;
using OrchestrationService.Contracts;
using OrchestrationService.Logger;
using OrchestrationService.OverlayNetworkStore;
using OrchestrationService.OverlayNetworkStore.Exceptions;
using Xunit.Sdk;

namespace OrchestrationServiceTests;

[TestClass]
public class SubnetAllocationTests
{
    [TestInitialize]
    public async Task SetUp()
    {
        var loggerFactory = LoggerFactory.Create(config =>
        {
            config.AddConsole();
        });
        OverlayNetworkLoggerProvider.Init(loggerFactory);
    }

    [TestMethod]
    public async Task SimpleCreate_AndGetTheSameSubnet()
    {
        var startingAddress = new int[]
        {
            10, 0, 0, 0
        };

        var subnet = new Subnet("tenant1", startingAddress, 24);
        var store = new FileOverlayNetworkSubnetStore();
        var success = await store.WriteSubnetMetadataToDb(subnet);
        Assert.IsTrue(success);

        var subnet2 = await store.GetSubnetMetadataFromDb(subnet.TenantName);
        Assert.AreEqual(subnet.TenantName, subnet2.TenantName);
        Assert.AreEqual(subnet.AddressSpace, subnet2.AddressSpace);
        for(var i=0; i < 4; i++)
        {
            Assert.AreEqual(subnet.MinAddress[i], subnet2.MinAddress[i]);
        }
    }

    [TestMethod]
    public async Task SimpleCreate_SubnetAlreadyExists()
    {
        var startingAddress = new int[]
        {
            10, 0, 0, 0
        };

        var subnet = new Subnet("tenant1", startingAddress, 24);
        var store = new FileOverlayNetworkSubnetStore();
        var success = await store.WriteSubnetMetadataToDb(subnet);
        Assert.IsTrue(success);
        var exceptionEncountered = false;
        try
        {
            await store.WriteSubnetMetadataToDb(subnet);
        }
        catch (SubnetAlreadyExistsException e)
        {
            exceptionEncountered = true;
        }

        Assert.IsTrue(exceptionEncountered);
    }

    [TestCleanup]
    public async Task CleanUp()
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "WireguardDb");
        Directory.Delete(dbPath, true);
    }
}