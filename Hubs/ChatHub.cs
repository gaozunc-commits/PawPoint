using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Demo.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public async Task SendMessage(string message)
    {
        var user = Context.User!.Identity!.Name!;
        var role = Context.User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
        var time = DateTime.Now.ToString("HH:mm");
        await Clients.All.SendAsync("ReceiveMessage", user, role, message, time);
    }
}
