
using System.Linq;
using Changelogs;
using Discord.WebSocket;
using Grpc.Core;
using SasavnServer.Controllers.Admin;
using SasavnServer.Controllers.ChangeLogs;
using SasavnServer.Repositories;
using SasavnServer.Service;

public class GChangelogsController : Changelogs.Changelogs.ChangelogsBase {

	private readonly ChangeLogsService changeLogsService;

	public GChangelogsController(
		ChangeLogsService changeLogsService
	)
	{
		this.changeLogsService = changeLogsService;
	}

	public async override Task<ChangelogsResponse> UploadChangelog(Changelog request, ServerCallContext context)
	{
		
		try {
			await changeLogsService.UploadUpdate(new ChangeLog {
				CreateDate = new DateTime(request.Timestamp),
				Data = request.Data.ToDictionary(pair => pair.Key, pair => pair.Value),
				Version = request.Version
			}, request.Publish, request.ChangeInDatabase);

		}
		catch(Exception ex) {
			
			return new ChangelogsResponse {
				Error = ex.Message,
				Result = -1
			};
		}

		return new ChangelogsResponse {
			Error = "OK",
			Result = 0
		};
	}
} 