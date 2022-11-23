using Microsoft.AspNetCore.Mvc;
using OrchestrationService.Contracts;
using OrchestrationService.Notifier;
using OrchestrationService.OverlayNetworkStore;
using OrchestrationService.OverlayNetworkStore.Exceptions;
using System.Security.Cryptography.X509Certificates;

namespace OrchestrationService.Controllers;

[ApiController]
[Route("[controller]")]
public class PeerClientController : ControllerBase
{
    public PeerClientController(ILogger<PeerClientController> logger)
    {
        _logger = logger;
        _overlayNetworkStore = FileOverlayNetworkStoreFactory.GetOrCreateDiskStore();
    }

    [HttpPost(Name = "OnboardNewPeerClient")]
    public async Task<ActionResult<Peer>> OnboardNewPeerClient(string tenantName, string publicKey)
    {
        _logger.LogInformation($"{nameof(OnboardNewPeerClient)}: new request received with params: {nameof(tenantName)}:{tenantName}, {nameof(publicKey)}:{publicKey}");
        Subnet subnet;
        try
        {
            subnet = await _overlayNetworkStore.GetSubnetMetadataFromDb(tenantName);
        }
        catch (SubnetNotFoundException e)
        {
            _logger.LogInformation($"{nameof(OnboardNewPeerClient)}: Could not find the following subnet: {tenantName}");
            return NotFound(e.Message);
        }

        var peer = new Peer(publicKey);

        bool success;
        try
        {
            success = await _overlayNetworkStore.FindAndAssignNewAddress(subnet, peer);
        }
        catch (SubnetIsFullException e)
        {
            _logger.LogInformation($"{nameof(OnboardNewPeerClient)}: Request failed since the subnet is full. {nameof(tenantName)}:{tenantName}, {nameof(publicKey)}:{publicKey}");
            return BadRequest(e);
        }

        if (success)
        {
            var notificationMessage = $"New peer joined with public key: {peer.PublicKey}, with private ip: {peer.GetReadableAddress()}";
            _logger.LogInformation($"{nameof(OnboardNewPeerClient)}: publishing message: {notificationMessage}");
            await WebSocketNotifier.Notify(tenantName, notificationMessage);
            return CreatedAtAction(nameof(OnboardNewPeerClient), peer, peer);
        }

        _logger.LogError($"{nameof(OnboardNewPeerClient)}: encountered unmapped error");
        // TODO:// more indicative error messages.
        return BadRequest("Something went wrong");
    }

    [HttpDelete(Name = "OffboardPeerClient")]
    public async Task<ActionResult> OffboardPeerClient(string tenantName, string privateIp, string token)
    {
        _logger.LogInformation($"{nameof(OffboardPeerClient)}: new request received with params: {nameof(tenantName)}:{tenantName}, {nameof(privateIp)}:{privateIp}");
        Subnet subnet;
        try
        {
            subnet = await _overlayNetworkStore.GetSubnetMetadataFromDb(tenantName);
        }
        catch (SubnetNotFoundException e)
        {
            _logger.LogInformation($"{nameof(OffboardPeerClient)}: Could not find the following subnet: {tenantName}");
            return NotFound(e.Message);
        }

        var privateIpParsed = Subnet.StringToIntArrayAddressConverter(privateIp);
        if (privateIpParsed.Length != 4)
        {
            _logger.LogInformation($"{nameof(OffboardPeerClient)}: invalid address length received: {nameof(tenantName)}:{tenantName}, {nameof(privateIp)}:{privateIp}");
            return BadRequest($"Invalid address length, received: {privateIp}, currretly only IPv4 is supported");
        }

        bool success;
        try
        {
            success = await _overlayNetworkStore.DeletePeerAsync(subnet, privateIpParsed, token);
        }
        catch(TokenMismatchException e)
        {
            _logger.LogInformation($"{nameof(OffboardPeerClient)}: token does not match for the request: {nameof(tenantName)}:{tenantName}, {nameof(privateIp)}:{privateIp}");
            return Forbid(e.Message);
        }
        catch (PeerNotFoundException e)
        {
            _logger.LogInformation($"{nameof(OffboardPeerClient)}: Could not find the following allocated private ip in the subnet: : {nameof(tenantName)}:{tenantName}, {nameof(privateIp)}:{privateIp}");
            return NotFound(e.Message);
        }

        if (success)
        {
            _logger.LogInformation($"{nameof(OffboardPeerClient)}: Peer left, notifying others: {nameof(tenantName)}:{tenantName}, {nameof(privateIp)}:{privateIp}");
            await WebSocketNotifier.Notify(tenantName, $"Peer left: private ip: {privateIp}");
            return Ok();
        }

        _logger.LogError($"{nameof(OffboardPeerClient)}: encountered unmapped error");
        // TODO:// more indicative error messages.
        return BadRequest("Something went wrong, could not delete the peer");
    }

    private readonly ILogger<PeerClientController> _logger;
    private readonly IOverlayNetworkStore _overlayNetworkStore;
}