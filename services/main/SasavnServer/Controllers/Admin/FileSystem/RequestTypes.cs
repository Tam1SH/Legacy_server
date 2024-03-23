
namespace SasavnServer.Controllers.Admin 
{
	public class UploadLauncherFilesModel {
		public IFormFile File { get; set; }
		public string FolderPath { get; set; }
	}

	public class RenameEntryModel 
	{
		public string OldEntryName { get; set; }
		public string NewEntryName { get; set; }
		public string FolderPath { get; set; }
	}
	
	public class CreateFolderModel 
	{
		public string FolderName { get; set; }
		public string FolderPath { get; set; }
	}
	public class DeleteFileModel 
	{
		public string FileName { get; set; }
		public string FolderPath { get; set; }

	}
	public class DownloadFileModel 
	{
		public string FileName { get; set; }
		public string FolderPath { get; set; }

	}
	public class GetFolderFilesModel
	{
		public string FolderPath { get; set; }
	}
}