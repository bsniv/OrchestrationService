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
        public OverlayNetworkController(ILogger<PeerClientController> logger)
        {
            _logger = logger;
            _overlayNetworkStore = FileOverlayNetworkStoreFactory.GetOrCreateStore();
        }

        [HttpPost(Name = "CreateNewNetwork")]
        public async Task<ActionResult<Subnet>> CreateNewNetwork(string tenantName, string minAddress, int addressSpace)
        {
            _logger.LogInformation($"{nameof(CreateNewNetwork)}: new request received with params: {nameof(tenantName)}:{tenantName}, {nameof(minAddress)}:{minAddress}, {nameof(addressSpace)}:{addressSpace}");
            var subnet = new Subnet(tenantName, minAddress, addressSpace);

            try
            {
                var success = await _overlayNetworkStore.WriteSubnetMetadataToDb(subnet);
                if (success)
                {
                    _logger.LogInformation($"{nameof(CreateNewNetwork)}: new request received with params: {nameof(tenantName)}:{tenantName}, {nameof(minAddress)}:{minAddress}, {nameof(addressSpace)}:{addressSpace} finished with success");
                    return CreatedAtAction(nameof(CreateNewNetwork), subnet, subnet);
                }
            }
            catch (SubnetAlreadyExistsException e)
            {
                _logger.LogInformation($"{nameof(CreateNewNetwork)}: new request received with params: {nameof(tenantName)}:{tenantName} failed, found an existing subnet");
                return Conflict(e.Message);
            }

            _logger.LogError($"{nameof(CreateNewNetwork)}: new request received with params: {nameof(tenantName)}:{tenantName} failed, unmapped issue");
            // TODO:// more implicative error handling.
            return BadRequest("Couldn't write subnet to db");
        }

        private readonly ILogger<PeerClientController> _logger;
        private readonly IOverlayNetworkStore _overlayNetworkStore;
    }
}