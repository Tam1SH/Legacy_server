using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SasavnServer.Controllers.Authenticate;
using SasavnServer.Model;
using Shared;

namespace SasavnServer.Repositories
{
    public class DataBaseContext : DbContext
    {
        public DbSet<User> Users { get; set; }
		public DbSet<LegacyUser> LegacyUser { get; set; }
		public DbSet<GameUser> GameUser { get; set; }
        public DbSet<SettingsData> Settings { get; set; }
        public DbSet<KeyCodes> Keycodes { get; set; }
        public DbSet<Reseller> Resellers { get; set; }
        public DbSet<AssetInfo> AssetInfo { get; set; }
        public DbSet<Updates> Updates { get; set; }
        public DbSet<Content> Content { get; set; }
        public DbSet<PresentImages> PresentImages { get; set; }
        public DbSet<Tokens> Tokens { get; set; }
        public DbSet<Stats> Stats { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Socials> Socials { get; set; }
        public DbSet<SocialUserCommon> SocialUserCommon { get; set; }
        public DataBaseContext(DbContextOptions<DataBaseContext> options) : base(options)
        {
        }

		public void Migrate() {

			foreach(var oldUser in LegacyUser) {

				var salo = AuthenticateService.HashingPassword(oldUser.Password);

				var roles = oldUser.Role;
				var role = "";

				if(roles == 4) {
					role = "User Helper Admin";
				}
				
				if(roles == 3) {
					role = "User Helper";
				}

				if(roles <= 2) {
					role = "User";
				}


				var _roles = role
					.Split(" ", StringSplitOptions.RemoveEmptyEntries)
					.Select(r => (UserRoles)Enum.Parse(typeof(UserRoles), r))
					.ToArray();

				Users.Add(new User {
						Id = oldUser.Id,
						Email = oldUser.Email,
						Login = oldUser.Login,
						Money = oldUser.Money,
						Password = salo.SaltedHashPass,
						Roles = _roles,
						Salt = salo.Salt,
					});

				if(oldUser.Role > 0) {

					GameUser.Add(new GameUser {
						HWID = oldUser.Hwid,
						HwidResetDate = oldUser.HwidResetDate,
						AuthToken = oldUser.AuthToken,
						UserId = oldUser.Id,
					});

					Subscriptions.Add(new Subscription {
						ActivationDate = DateTime.Now,
						AdminMessage = oldUser.AdminMessage,
						ExpirationDate = new DateTime(1970, 1, 1),
						GameId = Subscription.GameIdentificator.GTA5,
						UserId = oldUser.Id,
						TimeUpdate = oldUser.TimeUpdate
					});
					
				}

			}
			SaveChanges();
		}


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            modelBuilder
                .Entity<User>()
                .Property(u => u.Roles)
                .HasConversion<Converter>();
            
            base.OnModelCreating(modelBuilder);
        }
    }

    public class Converter : ValueConverter<UserRoles[], string>
    {
        public Converter()
            : base(
                roles => ConvertFromRoles(roles),
                roles => ConvertFromString(roles)
                )
        { }

		static public string ConvertFromRoles(UserRoles[] roles) => string.Join(" ", roles);

		static public UserRoles[] ConvertFromString(string roles) => roles
				.Split(' ')
				.Select(r => (UserRoles)Enum.Parse(typeof(UserRoles), r))
				.ToArray();
	}
}
