using Microsoft.AspNetCore.Mvc;
using OrchestrationService.Contracts;
using OrchestrationService.Notifier;
using OrchestrationService.OverlayNetworkStore;
using OrchestrationService.OverlayNetworkStore.Exceptions;

namespace OrchestrationService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PeerClientController : ControllerBase
    {
        private readonly ILogger<PeerClientController> _logger;
        private readonly IOverlayNetworkStore _overlayNetworkStore;

        public PeerClientController(ILogger<PeerClientController> logger)
        {
            _logger = logger;
            _overlayNetworkStore = FileOverlayNetworkStoreFactory.GetOrCreateStore();
        }

        [HttpPost(Name = "OnboardNewPeerClient")]
        public async Task<ActionResult<Peer>> OnboardNewPeerClient(string tenantName, string publicKey)
        {
            Subnet subnet;
            try
            {
                subnet = await _overlayNetworkStore.GetSubnetMetadataFromDb(tenantName);
            }
            catch (SubnetNotFoundException e)
            {
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
                return BadRequest(e);
            }

            if (success)
            {
                await WebSocketNotifier.Notify(tenantName, $"New peer joined with public key: {peer.PublicKey}, with private ip: {peer.GetReadableAddress()}");
                return CreatedAtAction(nameof(OnboardNewPeerClient), peer, peer);
            }

            // TODO:// more indicative error messages.
            return BadRequest("Something went wrong");
        }

        [HttpDelete(Name = "OffboardPeerClient")]
        public async Task<ActionResult> OffboardPeerClient(string tenantName, string privateIp, string token)
        {
            Subnet subnet;
            try
            {
                subnet = await _overlayNetworkStore.GetSubnetMetadataFromDb(tenantName);
            }
            catch (SubnetNotFoundException e)
            {
                return NotFound(e.Message);
            }

            var privateIpParsed = Subnet.StringToIntArrayAddressConverter(privateIp);
            if (privateIpParsed.Length != 4)
            {
                return BadRequest($"Invalid address length, received: {privateIp}, currretly only IPv4 is supported");
            }

            bool success;
            try
            {
                success = await _overlayNetworkStore.DeletePeerAsync(subnet, privateIpParsed, token);
            }
            catch(TokenMismatchException e)
            {
                return Forbid(e.Message);
            }
            catch (PeerNotFoundException e)
            {
                return NotFound(e.Message);
            }

            if (success)
            {
                await WebSocketNotifier.Notify(tenantName, $"Peer left: private ip: {privateIp}");
                return Ok();
            }

            // TODO:// more indicative error messages.
            return BadRequest("Something went wrong, could not delete the peer");
        }
    }
}