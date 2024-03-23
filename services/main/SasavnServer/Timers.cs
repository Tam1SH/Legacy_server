using SasavnServer.Controllers.Admin;

namespace SasavnServer
{
	public class Timers
    {

        private readonly IServiceProvider _serviceProvider;
        private readonly Timer StatsTimer;
        private readonly Timer OnlineCountTimer;

        public Timers(IServiceProvider serviceProvider) 
        {
            _serviceProvider = serviceProvider;

            StatsTimer = new Timer(StatsUpdate, null, TimeSpan.Zero, TimeSpan.FromHours(1));
            
			OnlineCountTimer = new Timer(OnlineCountUpdate, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        public async void StatsUpdate(object state)
        {
			using var scope = _serviceProvider.CreateScope();
			var adminService = scope.ServiceProvider.GetRequiredService<AdminService>();
			await adminService.UpdateStats();
			// do something with context

		}
        public async void OnlineCountUpdate(object state)
        {
            Usefull.usefull.RefreshOnlineCount();
        }
    }
}
