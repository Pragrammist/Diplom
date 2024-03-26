using Microsoft.AspNetCore.SignalR;

namespace WebHost.Hubs
{
    public class ProductsHub : Hub
    {
        public async Task SetIsLoaded(string productName)
        {
            await Clients.All.SendAsync("ProductLoaded", productName);
        }

    }
}
