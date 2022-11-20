using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace OrchestrationService.Notifier;

public class WebSocketNotifier
{
    public static async Task Notify(string tenantName, string message)
    {
        if (activeSockets.ContainsKey(tenantName))
        {
            var success = activeSockets.TryGetValue(tenantName, out var lst);
            if (success)
            {
                WebSocket[] clone;
                lock (lst)
                {
                    // copying all items to a temp array in order to block the lst for a long time.
                    clone = lst.ToArray();
                }

                var notifyTasks = clone.Select(webSocket =>
                {
                    // Todo:// remove socket from list in case it was closed.
                    if (webSocket.State == WebSocketState.Open)
                    {
                        ArraySegment<byte> json = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                        return webSocket.SendAsync(
                            json,
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                    }

                    return Task.CompletedTask;
                });

                await Task.WhenAll(notifyTasks);
            }
        }
    }

    public static void AddClient(string tenantName, WebSocket clientSocket)
    {
        var lst = activeSockets.GetOrAdd(tenantName, new List<WebSocket>());
        lock (lst)
        {
            lst.Add(clientSocket);
        }
    }

    public static ConcurrentDictionary<string, List<WebSocket>> activeSockets = new ConcurrentDictionary<string, List<WebSocket>>();
}
