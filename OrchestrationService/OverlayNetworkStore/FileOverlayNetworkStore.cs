using OrchestrationService.Contracts;

namespace OrchestrationService.OverlayNetworkStore;

public class FileOverlayNetworkStore : IOverlayNetworkStore
{
    public FileOverlayNetworkStore()// ILogger<FileOverlayNetworkStore> logger)
    {
        _addressStore = new FileOverlayNetworkAddressStore();
        _subnetStore = new FileOverlayNetworkSubnetStore();
        // _logger = logger;
    }

    public async Task<bool> FindAndAssignNewAddress(Subnet subnet, Peer peer)
    {
        return await _addressStore.FindAndAssignNewAddressAsync(subnet,peer);
    }

    public async Task<Subnet> GetSubnetMetadataFromDb(string tenantName)
    {
        return await _subnetStore.GetSubnetMetadataFromDb(tenantName);
    }

    public async Task<bool> WriteSubnetMetadataToDb(Subnet subnet)
    {
        return await _subnetStore.WriteSubnetMetadataToDb(subnet);
    }

    public async Task<bool> DeletePeerAsync(Subnet subnet, int[] address, string token)
    {
        return await _addressStore.DeletePeerAsync(subnet, address, token);
    }

    private readonly ILogger<FileOverlayNetworkStore> _logger;
    private readonly FileOverlayNetworkAddressStore _addressStore;
    private readonly FileOverlayNetworkSubnetStore _subnetStore;
}
