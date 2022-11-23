using Microsoft.AspNetCore.Http;
using OrchestrationService.Contracts;
using OrchestrationService.Logger;
using OrchestrationService.OverlayNetworkStore.DbClient;
using OrchestrationService.OverlayNetworkStore.Exceptions;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrchestrationService.OverlayNetworkStore;

public  class FileOverlayNetworkAddressStore
{
    public FileOverlayNetworkAddressStore(IDbClient dbClient)
    {
        _addressHandlers = new ConcurrentDictionary<string, FileOverlayNetworkAddressHandler>();
        _logger = OverlayNetworkLoggerProvider.GetLogger(nameof(FileOverlayNetworkAddressStore));
        _dbClient = dbClient;
    }


    /// <summary>
    /// Finds and allocates a new address for the given peer.
    /// </summary>
    /// <param name="peer"></param>
    /// <returns></returns>
    public async Task<bool> FindAndAssignNewAddressAsync(Subnet subnet, Peer peer)
    {
        _logger.LogInformation($"{nameof(FindAndAssignNewAddressAsync)}: Finding a new address to peer, tenantName: {subnet.TenantName}");
        var addressHandler = _addressHandlers.GetOrAdd(subnet.TenantName, new FileOverlayNetworkAddressHandler(subnet.TenantName, subnet, _dbClient));

        (var foundAddress, var address) = addressHandler.FindNewAddress();
        _logger.LogInformation($"{nameof(FindAndAssignNewAddressAsync)}: {nameof(foundAddress)}: {foundAddress}, tenantName: {subnet.TenantName}");
        if (!foundAddress || address == null)
        {
            return false;
        }

        var success = await addressHandler.AssignKnownAddressAsync(address, peer);
        _logger.LogInformation($"{nameof(FindAndAssignNewAddressAsync)}: Successfully assigned the new address: {success}, tenantName: {subnet.TenantName}");
        if (success)
        {
            peer.Subnet = subnet;
            peer.PrivateIp = address;
            return true;
        }

        return false;
    }

    public async Task<bool> DeletePeerAsync(Subnet subnet, int[] address, string token)
    {
        _logger.LogInformation($"{nameof(DeletePeerAsync)}: Deleting an address, address: {JsonSerializer.Serialize(address)}, tenantName: {subnet.TenantName}");
        var addressHandler = _addressHandlers.GetOrAdd(subnet.TenantName, new FileOverlayNetworkAddressHandler(subnet.TenantName, subnet, _dbClient));

        var peer = await addressHandler.GetPeerAsync(address);
        if (peer?.Token != token)
        {
            _logger.LogInformation($"{nameof(DeletePeerAsync)}: Invalid token received, address: {JsonSerializer.Serialize(address)}, tenantName: {subnet.TenantName}");
            throw new TokenMismatchException("Invalid token received, does not match the one saved on this address");
        }

        addressHandler.DeleteKnownAddress(address);
        _logger.LogInformation($"{nameof(DeletePeerAsync)}: Deleted an address, address: {JsonSerializer.Serialize(address)}, tenantName: {subnet.TenantName}");

        return true;
    }

    private ConcurrentDictionary<string, FileOverlayNetworkAddressHandler> _addressHandlers;
    private readonly ILogger _logger;
    private readonly IDbClient _dbClient;
}
