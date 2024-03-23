using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SasavnServer.Controllers.Admin;
using SasavnServer.Model;
using SasavnServer.Repositories;
using SasavnServer.Service;
using SasavnServer.usefull;

namespace SasavnServer.Controllers.ChangeLogs
{
	public class ChangeLog
    {
        public string Version { get; set; }
        public DateTime CreateDate { get; set; }
        public Dictionary<string, string> Data { get; set; }
        public IFormFile? File { get; set; }
    }

    public class ChangeLogsService
    {
        private readonly DiscordSocketClient discordClient;
        private readonly string discordToken;
		private readonly AdminService adminService;
        private readonly LoggerService<ChangeLogsService> logger;
		
        public ChangeLogsService(
			IConfiguration configuration,
            DiscordSocketClient discordClient,
			AdminService adminService,
			LoggerService<ChangeLogsService> logger)
        {
			this.adminService = adminService;
            this.discordClient = discordClient;
            this.logger = logger;
            discordToken = configuration["discordToken"]!;
			
            discordClient.Log += (msg) =>
            {
				logger.Info(new LogParams {
					Title = "Discord callback",
					Message = msg,
				});
				
                return Task.CompletedTask;
            };
        }

        public async Task UploadUpdate(ChangeLog changeLog, bool publish, bool changeInDatabase)
        {

			if (publish)
				await SendDiscordBotMessage(changeLog);

			if (changeInDatabase)
			{
				try
				{
					var versionWithoutShit = changeLog.Version.Split(" ")[0];
					await adminService.ChangeCheatVersion(versionWithoutShit);
				}
				catch (Exception ex)
				{
					logger.Error(new LogParams {
						Title = "Lol, menu version has not changed.",
						Message = ex
					});
					
				}
			}
        }

        async Task SendDiscordBotMessage(ChangeLog changelog)
        {
            try
            {
                await discordClient.LoginAsync(TokenType.Bot, discordToken);
                await discordClient.StartAsync();
                var body = "";
                body += "@everyone \n";
                //TODO: нужно ещё для лаунчера сделать
                body += $"Sasavn {changelog.Version} \n";
                body += $"--RU \n{changelog.Data["ru"]} \n\n";
                body += $"--EN \n{changelog.Data["en"]} \n";

				Console.WriteLine(body);
				
                //var result = await discordClient.GetChannelAsync(468837821923459084) as IMessageChannel;
                //await result.SendMessageAsync(body);
                await discordClient.LogoutAsync();

            }
            catch (Exception ex)
            {
				logger.Error(new LogParams {
					Title = "Lol, discord issue.",
					Message = ex
				});

            }
        }
    }
}
