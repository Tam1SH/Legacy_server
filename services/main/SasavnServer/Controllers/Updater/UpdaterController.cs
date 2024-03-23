
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;
using SasavnServer.Service;
using SasavnServer.usefull;

namespace SasavnServer.Controllers.Updater
{		

	[ApiExplorerSettings(IgnoreApi=true)]
	[OpenApiIgnore]
	[Route("api/[controller]")]
	//Updater for us launcher
	public class UpdaterController : ControllerBase {

		readonly PathResolver pathResolver;
		readonly LoggerService<UpdaterController> logger;

		public UpdaterController(
			IConfiguration configuration,
			IHttpContextAccessor httpContextAccessor,
			LoggerService<UpdaterController> logger
		) {
			this.logger = logger;
			pathResolver = new PathResolver(
				new PathString($"{configuration["storePath:path"]}LauncherFiles"),
				new Uri($"{httpContextAccessor.HttpContext.Request.Scheme}://{configuration["domain"]}/files/LauncherFiles")
			);
		}


		[HttpGet("update/{version}")]
		public async Task<ActionResult<UpdateResult>> Update(string version) {
			
			logger.Info(new LogParams {
				Message = $"search a new version for update {version}",
				Importance = Importance.NotExtremely
			});
			
			var directory = new DirectoryInfo(pathResolver.AbsolutePath());


			var latestVersion = directory.GetDirectories()
				.Select(dir => new Version(dir.Name))
				.OrderByDescending(v => v)
				.FirstOrDefault();
			
			
			if (latestVersion != null)
			{
				if(latestVersion < new Version(version)) {
					logger.Info(new LogParams {
						Message = $"received version greater then current, {latestVersion}, {version}",
						Importance = Importance.Extremely
					});
				}

				if(latestVersion.ToString() != version) {
					logger.Info(new LogParams {
						Message = $"New version was found {version}, {latestVersion}",
						Importance = Importance.NotExtremely
					});
				}
				else {
					logger.Info(new LogParams {
						Message = $"New version was not found {version}",
						Importance = Importance.NotExtremely
					});
					
					return NoContent();		

				}
			}
			else
			{						
				logger.Error(new LogParams {
					Message = $"latest version was not found {version}",
					Importance = Importance.NotExtremely
				});	

				return NoContent();		
			}

			var directoryWithLauncherFiles = pathResolver.AbsolutePath($"/{latestVersion}");
			
			var pathToLauncher = Directory.GetFiles(directoryWithLauncherFiles, "*.zip")[0];
			var pathToSigFile = Directory.GetFiles(directoryWithLauncherFiles, "*.sig")[0];

			var sigFile = await System.IO.File.ReadAllTextAsync(pathToSigFile);
			
			var urlToLauncher = pathResolver.PathSegmentToUrl($"/{Path.GetRelativePath(pathResolver.AbsolutePath(), pathToLauncher)}").ToString();
			

			var platform = new Dictionary<string, string>
			{
				{ "windows-x86_64", "" }
			};

			return Ok(new {
				Version = latestVersion,
				Notes = "",
				PubDate = new FileInfo(pathToSigFile).CreationTime,
				Platforms = new Dictionary<string, object>
				{
					["windows-x86_64"] = new
					{
						signature = sigFile,
						Url = urlToLauncher
					}
				},
			});
			
		}
	}
}
