using Microsoft.Extensions.Logging;
using OrchestrationService.Contracts;
using OrchestrationService.Logger;
using OrchestrationService.OverlayNetworkStore;
using OrchestrationService.OverlayNetworkStore.Exceptions;
using Xunit.Sdk;

namespace OrchestrationServiceTests;

[TestClass]
public class AddressAllocationTests
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
    public async Task SimpleAssignment_EasySpaceDvidies()
    {
        var startingAddress = new int[]
        {
            10, 0, 0, 0
        };

        var subnet = new Subnet("tenant1", startingAddress, 24);
        var peer = new Peer();
        var store = new FileOverlayNetworkAddressStore();
        var success = await store.FindAndAssignNewAddressAsync(subnet, peer);
        Assert.IsNotNull(peer.PrivateIp);
        Assert.IsTrue(success);
    }

    [TestMethod]
    public async Task SimpleAssignment_AddressSpacesStartFromMiddle()
    {
        var startingAddress = new int[]
        {
            10, 0, 4, 0
        };

        var subnet = new Subnet("tenant1", startingAddress, 24);
        var peer = new Peer();
        var store = new FileOverlayNetworkAddressStore();
        var success = await store.FindAndAssignNewAddressAsync(subnet, peer);
        Assert.IsNotNull(peer.PrivateIp);
        Assert.IsTrue(success);
    }

    [TestMethod]
    public async Task SimpleAssignment_AddressSpacesStartFromMiddleAndAddressSpaceIsNot8Multiplied()
    {
        var startingAddress = new int[]
        {
            10, 0, 4, 0
        };

        var subnet = new Subnet("tenant1", startingAddress, 22);
        var peer = new Peer();
        var store = new FileOverlayNetworkAddressStore();
        var success = await store.FindAndAssignNewAddressAsync(subnet, peer);
        Assert.IsNotNull(peer.PrivateIp);
        Assert.IsTrue(success);
    }

    [TestMethod]
    public async Task MultipleAssignments_DifferentIpsAllocated()
    {
        var startingAddress = new int[]
        {
            10, 0, 0, 0
        };

        var subnet = new Subnet("tenant1", startingAddress, 24);
        var peer1 = new Peer();
        var store = new FileOverlayNetworkAddressStore();
        var success = await store.FindAndAssignNewAddressAsync(subnet, peer1);
        Assert.IsTrue(success);
        Assert.IsNotNull(peer1.PrivateIp);
        var peer2 = new Peer();
        success = await store.FindAndAssignNewAddressAsync(subnet, peer2);
        Assert.IsTrue(success);
        Assert.IsNotNull(peer2.PrivateIp);
        Assert.AreNotEqual(peer1.PrivateIp[3], peer2.PrivateIp[3]);
    }

    [TestMethod]
    public async Task MultipleAssignments_FullSubnetExceptionEncountered()
    {
        var startingAddress = new int[]
        {
            10, 0, 0, 0
        };

        var subnet = new Subnet(Guid.NewGuid().ToString(), startingAddress, 30);
        var store = new FileOverlayNetworkAddressStore();
        bool success;
        for (var i=0; i<4; i++)
        {
            var peer = new Peer();
            success = await store.FindAndAssignNewAddressAsync(subnet, peer);
            Assert.IsTrue(success);
        }

        await Assert.ThrowsExceptionAsync<SubnetIsFullException>(() => store.FindAndAssignNewAddressAsync(subnet, new Peer()));
    }

    [TestMethod]
    public async Task MultipleAssignments_FullSuffix_ShouldContinueDfs()
    {
        var startingAddress = new int[]
        {
            10, 0, 0, 0
        };

        var subnet = new Subnet(Guid.NewGuid().ToString(), startingAddress, 16);
        var store = new FileOverlayNetworkAddressStore();
        bool success;
        Peer peer;
        for (var i = 0; i < 260; i++)
        {
            peer = new Peer();
            success = await store.FindAndAssignNewAddressAsync(subnet, peer);
            Assert.IsTrue(success);
        }

        peer = new Peer();
        success = await store.FindAndAssignNewAddressAsync(subnet, peer);
        Assert.AreEqual(peer.PrivateIp[2], 1);
    }

    [TestMethod]
    public async Task SimpleDelete_ShouldRemoveTheAllocation()
    {
        var startingAddress = new int[]
        {
            10, 0, 0, 0
        };

        var subnet = new Subnet("tenant1", startingAddress, 24);
        var peer = new Peer()
        {
            Token = "a"
        };
        var store = new FileOverlayNetworkAddressStore();
        var success = await store.FindAndAssignNewAddressAsync(subnet, peer);
        Assert.IsNotNull(peer.PrivateIp);
        Assert.IsTrue(success);

        success = await store.DeletePeerAsync(subnet, peer.PrivateIp, peer.Token);
        Assert.IsTrue(success);
    }

    [TestMethod]
    public async Task DeleteWithInvalidToken_ShouldAbortTheDeletion()
    {
        var startingAddress = new int[]
        {
            10, 0, 0, 0
        };

        var subnet = new Subnet("tenant1", startingAddress, 24);
        var peer = new Peer()
        {
            Token = "a"
        };
        var store = new FileOverlayNetworkAddressStore();
        var success = await store.FindAndAssignNewAddressAsync(subnet, peer);
        Assert.IsNotNull(peer.PrivateIp);
        Assert.IsTrue(success);

        var exceptionEncountered = false;
        try
        {
            await store.DeletePeerAsync(subnet, peer.PrivateIp, "b");
        }
        catch (TokenMismatchException e)
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