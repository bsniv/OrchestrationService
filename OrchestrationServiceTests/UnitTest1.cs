using OrchestrationService.Contracts;
using OrchestrationService.OverlayNetworkStore;
using Xunit.Sdk;

namespace OrchestrationServiceTests;

[TestClass]
public class UnitTest1
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
        Assert.IsTrue(success);
    }
}