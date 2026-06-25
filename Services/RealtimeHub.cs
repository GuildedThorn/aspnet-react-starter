using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace App.Services;

// Minimal SignalR hub. Anyone may connect to receive broadcasts; the server
// pushes events to clients (see NotesController, which calls
// IHubContext<RealtimeHub> to notify clients when notes change).
//
// Clients connect to "/hub" and listen for the "NoteChanged" event. Extend
// with your own client-invokable methods as needed.
[AllowAnonymous]
public class RealtimeHub : Hub {
    // Example client-invokable method: echo a ping back to the caller.
    public Task Ping() => Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
}
