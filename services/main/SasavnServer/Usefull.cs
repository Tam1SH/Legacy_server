using System.Security.Claims;
using SasavnServer.Repositories;
using SasavnServer.Controllers.Admin;
using SasavnServer.Controllers.Resellers;

namespace SasavnServer
{

	public class Usefull
    {
        private readonly IServiceProvider _serviceProvider;
        public static Usefull usefull;
        public Usefull(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            usefull = this;
        }


        static Random random = new();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

        }

        public static string GenKey(KeyType keyType)
        {
			throw new Exception("___SEC___");
        }
		
        public async void RefreshOnlineCount() //better do that every min, idk how atm
        {
            
            try
            {

                using var scope = _serviceProvider.CreateScope();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var adminService = scope.ServiceProvider.GetRequiredService<AdminService>();
                var usersOnline = await adminService.GetUsersOnlineAsync();

                userRepository.BeginTransaction((trans) =>
                {
                    var settings = userRepository.Settings().Single();
                    settings.OnlineCount = usersOnline.Length;
                    trans.Update(settings);
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"RefreshOnlineCount: {ex.Message}");
            }
            
        }
		
    }


    static class ExtForIdentity
    {
        public static User? GetUserData(this ClaimsPrincipal user)
        {

            var id = user.Identities
				.Where(claims => claims.HasClaim(ClaimTypes.Role, "User"))
				.FirstOrDefault();

			if(id == null)
				return null;

            return User.FromIdentity(id);

        }
    }
}
