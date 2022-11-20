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
    private readonly IOverlayNetworkStore _overlayNetworkStore;

    public WebSocketController()
    {
        _overlayNetworkStore = FileOverlayNetworkStoreFactory.GetOrCreateStore();
    }

    [HttpGet(Name = "GetWebSocket")]
    public async Task<ActionResult> Get(string tenantName)
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

        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            WebSocketNotifier.AddClient(tenantName, webSocket);
            
            await Echo(webSocket);
        }
        else
        {
            return BadRequest("Expected to be web socket request");
        }

        return Ok();
    }

    private static async Task Echo(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            await webSocket.SendAsync(
                new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                receiveResult.MessageType,
                receiveResult.EndOfMessage,
                CancellationToken.None);

            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
    }
}