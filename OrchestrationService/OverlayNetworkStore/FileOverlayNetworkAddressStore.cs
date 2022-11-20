using Microsoft.AspNetCore.Http;
using OrchestrationService.Contracts;
using OrchestrationService.OverlayNetworkStore.Exceptions;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrchestrationService.OverlayNetworkStore;

public  class FileOverlayNetworkAddressStore
{
    public FileOverlayNetworkAddressStore()
    {
        _addressHandlers = new ConcurrentDictionary<string, FileOverlayNetworkAddressHandler>();
//        _storePath = Path.Combine(Directory.GetCurrentDirectory(), "WireguardDb", subnet.TenantName);

//        Directory.CreateDirectory(_storePath);
    }


    /// <summary>
    /// Finds and allocates a new address for the given peer.
    /// </summary>
    /// <param name="peer"></param>
    /// <returns></returns>
    public async Task<bool> FindAndAssignNewAddressAsync(Subnet subnet, Peer peer)
    {
        var storePath = Path.Combine(Directory.GetCurrentDirectory(), "WireguardDb", subnet.TenantName);
        var addressHandler = _addressHandlers.GetOrAdd(subnet.TenantName, new FileOverlayNetworkAddressHandler(storePath, subnet));

        (var foundAddress, var address) = addressHandler.FindNewAddress();
        if (!foundAddress || address == null)
        {
            return false;
        }

        var success = await addressHandler.AssignKnownAddressAsync(address, peer);
        if (success)
        {
            peer.Subnet = subnet;
            peer.PrivateIp = address;
        }

        return true;
    }

    /// <summary>
    /// Finds and allocates a new address for the given peer.
    /// </summary>
    /// <param name="peer"></param>
    /// <returns></returns>
    public async Task<bool> DeleteAddressAsync(Subnet subnet, Peer peer)
    {
        var storePath = Path.Combine(Directory.GetCurrentDirectory(), "WireguardDb", subnet.TenantName);
        var addressHandler = _addressHandlers.GetOrAdd(subnet.TenantName, new FileOverlayNetworkAddressHandler(storePath, subnet));

        (var foundAddress, var address) = addressHandler.FindNewAddress();
        if (!foundAddress || address == null)
        {
            return false;
        }

        var success = await addressHandler.AssignKnownAddressAsync(address, peer);
        if (success)
        {
            peer.Subnet = subnet;
            peer.PrivateIp = address;
        }

        return true;
    }

    public async Task<bool> DeletePeerAsync(Subnet subnet, int[] address, string token)
    {
        var storePath = Path.Combine(Directory.GetCurrentDirectory(), "WireguardDb", subnet.TenantName);
        var addressHandler = _addressHandlers.GetOrAdd(subnet.TenantName, new FileOverlayNetworkAddressHandler(storePath, subnet));

        var peer = await addressHandler.GetPeerAsync(address);
        if (peer?.Token != token)
        {
            throw new TokenMismatchException("Invalid token received, does not match the one saved on this address");
        }

        addressHandler.DeleteKnownAddress(address);

        return true;
    }

    private ConcurrentDictionary<string, FileOverlayNetworkAddressHandler> _addressHandlers;
}
