using Microsoft.AspNetCore.Mvc;
using OrchestrationService.Contracts;
using OrchestrationService.OverlayNetworkStore;
using OrchestrationService.OverlayNetworkStore.Exceptions;
using System.Text.Json;

namespace OrchestrationService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OverlayNetworkController : ControllerBase
    {
        private readonly ILogger<PeerClientController> _logger;
        private readonly IOverlayNetworkStore _overlayNetworkStore;

        public OverlayNetworkController(ILogger<PeerClientController> logger)
        {
            _logger = logger;
            _overlayNetworkStore = FileOverlayNetworkStoreFactory.GetOrCreateStore();
        }

        [HttpPost(Name = "CreateNewNetwork")]
        public async Task<ActionResult<Subnet>> CreateNewNetwork(string tenantName, string minAddress, int addressSpace)
        {
            var subnet = new Subnet(tenantName, minAddress, addressSpace);

            try
            {
                var success = await _overlayNetworkStore.WriteSubnetMetadataToDb(subnet);
                if (success)
                {
                    return CreatedAtAction(nameof(CreateNewNetwork), subnet, subnet);
                }
            }
            catch (SubnetAlreadyExistsException e)
            {
                return Conflict(e.Message);
            }

            // TODO:// more implicative error handling.
            return BadRequest("Couldn't write subnet to db");
        }
    }
}