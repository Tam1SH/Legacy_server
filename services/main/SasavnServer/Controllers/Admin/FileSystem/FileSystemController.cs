
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SasavnServer.ApiClasses;

namespace SasavnServer.Controllers.Admin

{
	[Authorize]
	[ClaimRequirement(ClaimTypes.Role, "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class FileSystemController : Controller
    {
		private readonly FileSystemService service;

		public FileSystemController(
			FileSystemService service
		)
		{
			this.service = service;
		}


		[HttpPost("renameEntry")]
		public void RenameEntry([FromBody] RenameEntryModel model) 
		{
			service.RenameEntry(model.OldEntryName, model.NewEntryName, model.FolderPath);
		}

		[HttpPost("deleteEntry")]
		public void DeleteEntry([FromBody] DeleteFileModel model)
		{
			service.DeleteEntry(model.FileName, model.FolderPath);
		}

		[HttpPost("downloadFile")]
		public async Task<FileContentResult> DownloadFile([FromBody] DownloadFileModel model) {

			var bytes = await service.DownloadFile(model.FileName, model.FolderPath);

			return File(bytes, "application/zip", model.FileName);
		}
		[HttpPost("createFolder")]
		public void CreateFolder([FromBody] CreateFolderModel model)
		{
			service.CreateFolder(model.FolderName, model.FolderPath);
		}

		[RequestSizeLimit(long.MaxValue)]
		[HttpPost("uploadLauncherFiles")]
		public async Task UploadLauncherFiles([FromForm] UploadLauncherFilesModel model) 
		{
			await service.UploadLauncherFiles(model.File, model.FolderPath);
		}

		[HttpPost("GetFolderFiles")]
		public ActionResult<Entry> GetFolderFiles([FromBody] GetFolderFilesModel model) {

			var files = service.GetFolderFiles(model.FolderPath);
			
			if(files is null)
				return BadRequest(new ErrorCode(-1, ""));
			
			return Ok(files);
		}


		[HttpGet("GetAllFiles")]
		public ActionResult<Entry> GetAllFiles()
		{

			var files = service.GetFilesStructure();

			if (files is null)
				return BadRequest(new ErrorCode(-1, ""));

			return Ok(files);

		}

	}
}