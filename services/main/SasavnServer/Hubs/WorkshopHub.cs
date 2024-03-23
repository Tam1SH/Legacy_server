using Microsoft.AspNetCore.SignalR;

namespace SiteAPI.Hubs
{
	public interface IWorkshopClient
    {
        Task ChangedAsset(long id);
        Task RemovedAsset(long id);
    }

    public class WorkshopHub : Hub<IWorkshopClient>
    { }
}
