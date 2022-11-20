using OrchestrationService.Contracts;
using OrchestrationService.OverlayNetworkStore;
using OrchestrationService.OverlayNetworkStore.Exceptions;
using Xunit.Sdk;

namespace OrchestrationServiceTests;

[TestClass]
public class AddressAllocationTests
{
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

    [TestCleanup]
    public async Task CleanUp()
    {
        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "WireguardDb");
        Directory.Delete(dbPath, true);
    }
}