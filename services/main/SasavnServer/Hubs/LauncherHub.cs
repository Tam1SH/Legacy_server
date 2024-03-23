//using Emzi0767.Utilities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace SiteAPI.Hubs
{
	public interface ILauncherClient
    {
        Task WaitForLogin(string accessToken, string refreshToken, string expiration, string password);
    }

    public class LauncherHub : Hub<ILauncherClient>
    {
        private readonly IMemoryCache cacheUsers;


        public LauncherHub(IMemoryCache memoryCache)
        {
            this.cacheUsers = memoryCache;
        }

        public void DeleteByKey(string key) 
        {
            cacheUsers.Remove(key);
        }

        public string GetConnectionId(string uuid)
        {
            return (string)cacheUsers.Get(uuid);
        }

        public void RegistrationUUID(string uuid)
        {
            cacheUsers.Set(uuid, Context.ConnectionId,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));

        }

    }
}
