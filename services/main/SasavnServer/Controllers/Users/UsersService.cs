using Microsoft.EntityFrameworkCore;
using SasavnServer.ApiClasses;
using SasavnServer.Repositories;
using SasavnServer.Service;
using SasavnServer.usefull;
using SiteAPI.Hubs;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Net.Http.Headers;
using Image = SixLabors.ImageSharp.Image;

namespace SasavnServer.Controllers.Users
{

	public class UsersService
    {

        private readonly LoggerService<UsersService> logger;
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly LauncherHub _launcherHub;
        private readonly PathResolver pathResolver;

        public UsersService(
			IConfiguration configuration, 
			IUserRepository userRepository, 
			LauncherHub launcherHub,
			LoggerService<UsersService> logger
			)
        {
			this.logger = logger;
            _launcherHub = launcherHub;
            _configuration = configuration;
            _userRepository = userRepository;
            pathResolver = new PathResolver(
                new PathString($"{configuration["storePath:path"]}"),
                new Uri($"https://{configuration["domain"]}/files")
                );
        }



        public Uri? GetUserProfileImage(string username)
        {
            var path = pathResolver.AbsolutePath("/UsersProfileImages");

            var images = Directory.EnumerateFiles(path)
                .Where(image => image.Contains(username))
                .SingleOrDefault();

            if (images == null)
                return null;

            var relativePath = new PathString($"/{Path.GetRelativePath(pathResolver.AbsolutePath(), images)}");

            return pathResolver.PathSegmentToUrl(relativePath);
        }

        private static MemoryStream ResizeImageTo150x150AndReturnStream(MemoryStream imageStream)
        {
            var image = Image.Load(imageStream);
            image.Mutate(img => img.Resize(150, 150));
            imageStream.Dispose();
            var ms = new MemoryStream();
            image.Save(ms, JpegFormat.Instance);
            ms.Position = 0;
            return ms;
        }

        async public Task<ErrorCode?> LoadProfileImage(IFormFile image, User user)
        {

            var path = pathResolver.AbsolutePath("/UsersProfileImages");

            var extension = ContentDispositionHeaderValue.Parse(image.ContentDisposition).FileName!.Trim('"');

            extension = Path.GetExtension(extension);

            if (extension != ".jpg" && extension != ".png" && extension != ".jpeg")
                return new ErrorCode(-3, "failed load image");

            var imagePath = Path.Combine(path, $"{user.Login}{extension}");

            var imageName = Path.GetFileNameWithoutExtension(imagePath);

            DeleteALlFilesWithSubstring(imageName);

            using (var imageStream = File.Create(imagePath))
            {
                var imageMemoryStream = new MemoryStream();
                await image.CopyToAsync(imageMemoryStream);
                imageMemoryStream.Position = 0;
                var result = ResizeImageTo150x150AndReturnStream(imageMemoryStream);

                await result.CopyToAsync(imageStream);
                await result.DisposeAsync();
            }

            return null;

        }


        public async Task SendAuthDataToLauncher(string uuid, string accessToken, string refreshToken, string expiration, string password)
        {
            var con = _launcherHub.GetConnectionId(uuid);
            await _launcherHub.Clients.Client(con).WaitForLogin(accessToken, refreshToken, expiration, password);
            _launcherHub.DeleteByKey(uuid);
        }

        public async Task<ErrorCode?> ActivateKey(string key, User user_)
        {
			
			return await _userRepository.BeginTransaction(async (trans) => {

				var user = await trans
					.All()
					.SingleAsync(u => u.Login == user_.Login);

				var keyCode = await trans
					.GetAllKeys()
					.FirstAsync(k => k.KeyName == key);

				if(keyCode is null)
					return new ErrorCode(-3, "Invalid key");
					
				if (keyCode.Activated == 1)
					return new ErrorCode(-4, "Key Arleady Activated");
				if (keyCode.Permissions == 0)
					return new ErrorCode(-5, "Key Has No Permissions");

			
				
				var reseller = await trans
					.Resellers()
					.SingleAsync(r => r.Login == keyCode.Reseller);

				if (reseller.Official == 0)
					return new ErrorCode(-8, "Broken key, reseller are banned");

				var gameUser = await trans
					.GameUsers()
					.SingleOrDefaultAsync(u => u.UserId == user.Id);

				if (gameUser == null) {
					logger.Info(new LogParams {
						Message = $"New GameUser for {user.Login}",
						UserId = user.Id,
						Importance = Importance.Extremely,
					});
				}
					
				gameUser ??= trans.Add(new GameUser
				{
					UserId = user.Id,
					AuthToken = "",
					HWID = "",
					HwidResetDate = DateTime.Now
				});

				if (keyCode.Permissions != 4)
				{
					var subs = trans
						.Subscriptions()
						.Where(s => s.GameId == Subscription.GameIdentificator.GTA5 && s.UserId == user.Id)
						.ToArray();

					if(subs.Length >= 1) {
						return new ErrorCode(-10, "Subscription already exist");
					}
					
					//TODO: Only for GTA5
					var newSub = new Subscription {
						ActivationDate = DateTime.Now,
						AdminMessage = "Nothing",
						ExpirationDate = new DateTime(1970, 1, 1),
						GameId = Subscription.GameIdentificator.GTA5,
						TimeUpdate = DateTime.Now,
						UserId = user.Id
					};

					logger.Info(new LogParams {
						Message = "Adding subscription",
						UserId = user.Id,
						Importance = Importance.Extremely,
					});
					
					trans.Add(newSub);

					reseller.ActivatedKeys++;
				}
				else
				{


					gameUser.HwidResetDate = DateTime.Now.AddDays(-7);
					reseller.ActivatedHwidKeys++;

					trans.Update(gameUser);
				}

				trans.Update(reseller);

				
				keyCode.Activated = 1;
				keyCode.Activatedby = user.Login;
				
				trans.Update(user);
				trans.Update(keyCode);
				
				return null;
			});
        }


		
        private void DeleteALlFilesWithSubstring(string str)
        {
            var path = pathResolver.AbsolutePath("/UsersProfileImages");
            foreach (var file in Directory.EnumerateFiles(path))
            {
                if (file != null)
                    if (file.IndexOf(str) > 0)
                        File.Delete(file);
            }
        }

    }
}
