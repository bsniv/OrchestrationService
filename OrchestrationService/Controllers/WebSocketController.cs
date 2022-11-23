using Microsoft.AspNetCore.Mvc;
using OrchestrationService.Contracts;
using OrchestrationService.Notifier;
using OrchestrationService.OverlayNetworkStore;
using OrchestrationService.OverlayNetworkStore.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace OrchestrationService.Controllers;

[ApiController]
[Route("[controller]")]
public class WebSocketController : ControllerBase
{
    public WebSocketController(ILogger<WebSocketController> logger)
    {
        _overlayNetworkStore = FileOverlayNetworkStoreFactory.GetOrCreateDiskStore();
        _logger = logger;
    }

    [HttpGet(Name = "GetWebSocket")]
    public async Task<ActionResult> GetWebSocket(string tenantName)
    {
        _logger.LogInformation($"{nameof(GetWebSocket)}: new request received: {nameof(tenantName)}:{tenantName}");
        Subnet subnet;
        try
        {
            subnet = await _overlayNetworkStore.GetSubnetMetadataFromDb(tenantName);
        }
        catch (SubnetNotFoundException e)
        {
            _logger.LogInformation($"{nameof(GetWebSocket)}: Could not find the following subet: {tenantName}");
            return NotFound(e.Message);
        }

        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            WebSocketNotifier.AddClient(tenantName, webSocket);
            await WaitForCloseSignal(webSocket);
            _logger.LogInformation($"{nameof(GetWebSocket)}: Successfully initialized a web socket connection and added the client: {tenantName}");
        }
        else
        {
            _logger.LogInformation($"{nameof(GetWebSocket)}: Received a non web socket request: {tenantName}");
            return BadRequest("Expected to be web socket request");
        }

        return Ok();
    }

    private static async Task WaitForCloseSignal(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
    }

    private readonly ILogger<WebSocketController> _logger;
    private readonly IOverlayNetworkStore _overlayNetworkStore;
}