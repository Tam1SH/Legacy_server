using Microsoft.EntityFrameworkCore;
using SasavnServer.Controllers.Users;
using SasavnServer.Repositories;
using SasavnServer.usefull;
using Serilog;
using Shared;
using ILogger = Serilog.ILogger;
using MoreLinq;

namespace SasavnServer.Controllers.Admin
{

	public class AdminService
    {
        private readonly ILogger logger = Log.ForContext<AdminService>();
        private readonly IUserRepository _userRepository;
        private readonly PathResolver pathResolver;

        public AdminService(IConfiguration configuration, IUserRepository userRepository)
        {

            _userRepository = userRepository;
            pathResolver = new PathResolver(
                new PathString($"{configuration["storePath:path"]}"),
                new Uri($"https://{configuration["domain"]}/files")
                );
        }

        public async Task UpdateStats()
        {
			var lol = await GetSettingsData();

			_userRepository.Add(new Stats
			{
				Date = DateTime.Now,
				OnlineCount = lol.OnlineCount,
				TotalLogins = lol.TotalLogins,
				TotalUsedTime = lol.TotalUsedTime,
			});

        }

        public Task<Stats[]> GetStats()
        {
            return _userRepository.Stats().ToArrayAsync();
        }


        public async Task<string> GetLogs()
        {
            var texts = "";
            var root = $"{Directory.GetCurrentDirectory()}/Logs";

            foreach (var logPath in Directory.EnumerateFiles(root)) {

                var relativePath = Path.GetRelativePath(root, logPath);

                if(relativePath.StartsWith("my-logs"))
                {
                    using var stream = File.Open(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);
                    var text = await reader.ReadToEndAsync();
                    texts += text + '\n';
                }
            }

            return texts;
        }

        public async Task<AllMainInfoAboutSHBHLOL> AllMainInfo()
        {
            var info = new AllMainInfoAboutSHBHLOL();
            var lol = await GetSettingsData();
            info.TotalLogins = lol.TotalLogins;
            info.LauncherVersion = lol.LauncherVersion;
            info.MenuVersion = lol.SasavnGTAVerison;
            info.TotalNonFreeUsers = (await GetNonFreeUsersAsync()).Length;
            info.TotalUsers = (await GetAllUsersAsync()).Length;
            info.TotalTime = lol.TotalUsedTime;
            info.UsersOnline = (await GetUsersOnlineAsync()).Length;
            return info;
        }

        public async Task ChangeLauncherVersion(string newVersion)
        {
            await _userRepository.BeginTransaction(async (trans) => {

                var settings = await GetSettingsData();
                settings.LauncherVersion = newVersion;
                trans.Update(settings);
            });

        }

        public async Task ChangeCheatVersion(string newVersion, UserRepositoryContextTransaction? trans = null)
        {
            logger.Information($"Change cheat version : {newVersion}");
            if(trans != null)
            {
                var settings = await GetSettingsData();
                settings.SasavnGTAVerison = newVersion;
                trans.Update(settings);
            }
            else await _userRepository.BeginTransaction(async (trans) => {
				
                var settings = await GetSettingsData();
                settings.SasavnGTAVerison = newVersion;
                trans.Update(settings);
			});
        }

        public async Task ChangeUser(PartialUser userData)
        {
            throw new NotImplementedException();
            logger.Information($"ChangeUser, login : {userData.Login}");

            using var trans = _userRepository.BeginTransaction();
            try
            {
                var user = await _userRepository.All().SingleAsync(u => u.Id == userData.Id);

                user.Email = userData.Email ?? user.Email;
                //user.Pass = userData.Password ?? user.Pass;
                // user.Role = userData.Role ?? user.Role;
                //user.DSUserID = userData.DSUserID ?? user.DSUserID;
                //user.DSUserName = userData.DSUserName ?? user.DSUserName;
                //user.VKId = userData.VKId ?? user.VKId;
                //user.VKTempKey = userData.VKTempKey ?? user.VKTempKey;
                //user.Avatar = userData.Avatar ?? user.Avatar;


                await trans.CommitAsync();
                _userRepository.Save();
                logger.Information($"ChangeUser success, login : {userData.Login}");
            }
            catch (Exception ex)
            {

                logger.Error($"ChangeUser error, login : {userData.Login}, message : {ex.Message}");
                await trans.RollbackAsync();
                throw;
            }
        }

        public async Task<SettingsData> GetSettingsData()
        {
            var settings = await _userRepository.Settings().SingleAsync();
            return settings;
        }

        public async Task<int> GetTotalLogins()
        {
            var settings = await _userRepository.Settings().SingleAsync();
            return settings.TotalLogins;
        }

        public async Task<string> GetCheatGTAVersion()
        {
            var settings = await _userRepository.Settings().SingleAsync();
            return settings.SasavnGTAVerison;
        }

        public async Task<string> GetLauncherVersion()
        {
            var settings = await _userRepository.Settings().SingleAsync();
            return settings.LauncherVersion;
        }

        public async Task<TotalUser[]> GetUsersOnlineAsync()
        {
            var subscriptions = await _userRepository
				.Subscriptions()
                .Where(u => u.TimeUpdate.AddMinutes(5) > DateTime.Now)
                .ToArrayAsync();

            var gameUsers = _userRepository
                .GameUsers();

            var users = _userRepository.All();

            return subscriptions
                .Join(gameUsers, 
                    subscription => subscription.UserId,
                    gameUser => gameUser.UserId, 
                    (subscription, gameUser) => new { Subscription = subscription, GameUser = gameUser })
                .Join(users, 
                    sg => sg.Subscription.UserId, 
                    user => user.Id, 
                    (sg, user) => new { SubscriptionGameUser = sg, User = user })
                .Select(x => new TotalUser
                {
                    Id = x.User.Id,
                    Login = x.User.Login,
                    Email = x.User.Email,
                    Salt = x.User.Salt,
                    Money = x.User.Money,
                    Roles = x.User.Roles.Select(r => r.ToString()).ToArray(),
                    HWID = x.SubscriptionGameUser.GameUser.HWID,
                    AuthToken = x.SubscriptionGameUser.GameUser.AuthToken,
                    HwidResetDate = x.SubscriptionGameUser.GameUser.HwidResetDate,
                    Subscriptions = new Subscription[] { x.SubscriptionGameUser.Subscription }
                })
                .ToArray();
        }

		public async Task<TotalUser[]> GetAllUsersAsync()
        {
            var users = await _userRepository.All().AsNoTracking().AsAsyncEnumerable()
                    .ToArrayAsync();
			
			var gameUsers = await _userRepository
				.GameUsers()
				.ToArrayAsync();
			
			var subs = await _userRepository
				.Subscriptions()
				.ToArrayAsync();

			var result = users
				.GroupJoin(gameUsers, user => user.Id, gameUser => gameUser.UserId, (user, gameUserGroup) => new { user, gameUserGroup })
				.GroupJoin(subs, u => u.user.Id, subscription => subscription.UserId, (u, subscriptionGroup) => new TotalUser
				{
					Id = u.user.Id,
					Login = u.user.Login,
					Email = u.user.Email,
					Salt = u.user.Salt,
					Money = u.user.Money,
					Roles = u.user.Roles.Select(r => r.ToString()).ToArray(),
					HWID = u.gameUserGroup.FirstOrDefault()?.HWID,
					AuthToken = u.gameUserGroup.FirstOrDefault()?.AuthToken,
					HwidResetDate = u.gameUserGroup.FirstOrDefault()?.HwidResetDate,
					Subscriptions = subscriptionGroup.ToArray()
				})
				.ToArray();
			
			return result;
        }
        public async Task<Subscription[]> GetNonFreeUsersAsync()
        {
             return await _userRepository.Subscriptions().ToArrayAsync();
        }
        public User[] getUsersById(int offset, int count)
        {
            var users = _userRepository.All().AsNoTracking().AsEnumerable()
                .Take(new Range(offset, offset + count))
                .ToArray();

            foreach (var user in users)
            {
                user.Password = "";
				user.Salt = "";
            }

            return users;
        }

    }
}
