using OrchestrationService.Contracts;

namespace OrchestrationService.OverlayNetworkStore;

public interface IOverlayNetworkStore
{
    public Task<bool> FindAndAssignNewAddress(Subnet subnet, Peer peer);

    public Task<Subnet> GetSubnetMetadataFromDb(string tenantName);

    public Task<bool> WriteSubnetMetadataToDb(Subnet subnet);

    public Task<bool> DeletePeerAsync(Subnet subnet, int[] address, string token);
}
