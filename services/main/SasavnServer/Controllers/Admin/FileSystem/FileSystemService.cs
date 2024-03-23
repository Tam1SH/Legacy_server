
using System.IO.Compression;
using SasavnServer.Service;
using SasavnServer.usefull;

namespace SasavnServer.Controllers.Admin 
{
	public class FileSystemService {

        private readonly PathResolver pathResolver;
		private readonly LoggerService<FileSystemService> logger;
		public FileSystemService(
			IConfiguration configuration,
			LoggerService<FileSystemService> logger
		) 
		{
			this.logger = logger;
			pathResolver = new PathResolver(
                new PathString($"{configuration["storePath:path"]}"),
                new Uri($"https://{configuration["domain"]}/files")
                );
		}


		public void RenameEntry(string oldEntryName, string newEntryName, string FolderPath) 
		{
			string sourceEntryPath = Path.Combine(FolderPath, oldEntryName);
			string destinationEntryPath = Path.Combine(FolderPath, newEntryName);
			if (File.Exists(sourceEntryPath))
			{
				File.Move(sourceEntryPath, destinationEntryPath);
			}
			else if (Directory.Exists(sourceEntryPath)) 
			{
   				Directory.Move(sourceEntryPath, destinationEntryPath);
			}
			else throw new FileNotFoundException();
		} 

		public void CreateFolder(string FolderName, string FolderPath) 
		{
			Directory.CreateDirectory(Path.Combine(FolderPath, FolderName));
		}

		public void DeleteEntry(string FileName, string FolderPath) {
			
			var path = Path.Combine(FolderPath, FileName);

			if (File.Exists(path))
			{
				File.Delete(path);
			}
			else if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}
			else throw new FileNotFoundException();
		}		

		public async Task<byte[]> DownloadFile(string FileName, string FolderPath) {
			
			var path = Path.Combine(FolderPath, FileName);

			using var file = File.Open(path, FileMode.Open);
			using var str = new MemoryStream();
			await file.CopyToAsync(str);

			byte[] fileBytes = str.ToArray();
			byte[]? compressedBytes = null;

			using (var outStream = new MemoryStream())
			{
				using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true))
				{
					var fileInArchive = archive.CreateEntry(path, CompressionLevel.Optimal);
					using var entryStream = fileInArchive.Open();
					using var fileToCompressStream = new MemoryStream(fileBytes);
					fileToCompressStream.CopyTo(entryStream);

				}
				compressedBytes = outStream.ToArray();
			}

			return compressedBytes;
		}

		public async Task UploadLauncherFiles(IFormFile file, string FolderPath) {

			using var stream = file.OpenReadStream();
            using var Z = new ZipArchive(stream, ZipArchiveMode.Read);

            foreach (var entry in Z.Entries)
			{		
				using var fileStream = entry.Open();
				using var file_ = File.Open(Path.Combine(FolderPath, entry.Name), FileMode.OpenOrCreate);
				await fileStream.CopyToAsync(file_);
            }
			
		}

		public Entry? GetFilesStructure()
		{

			var root = pathResolver.AbsolutePath();

			Entry? rootEntry = new Entry { Name = root, IsFile = false };
			//TraverseDirectory(root, ref rootEntry);

			return rootEntry;
		}

		public Entry? GetFolderFiles(string path)
		{
			try
			{
        		string[] entries = Directory.GetFileSystemEntries(path);

				Entry? rootEntry = new()
				{
					Name = path,
					IsFile = false,
					Children = new Entry[entries.Length]
				};

				for (int i = 0; i < entries.Length; i++)
				{
					string fileName = Path.GetFileName(entries[i]);
					
					var meta = Directory.Exists(entries[i])
						? new Func<MetaData>(() => {
							var directoryInfo = new DirectoryInfo(entries[i]);

							return new MetaData {
								CreationDate = directoryInfo.CreationTimeUtc.ToString()
							};
						})()
					
						: new Func<MetaData>(() => {
							
							var fileInfo = new FileInfo(entries[i]);

							return new MetaData {
								CreationDate = fileInfo.CreationTimeUtc.ToString(),
								Length = fileInfo.Length
							};
						})();

        			var fileInfo = new FileInfo(entries[i]);

            		bool isFile = File.Exists(entries[i]);
					
					rootEntry.Children[i] = new Entry { 
						Name = fileName, 
						IsFile = isFile,
						MetaData = meta
					};
				}

				return rootEntry;
			}
			catch (Exception ex)
			{
				logger.Error(new LogParams {
					Title = "Error retrieving folder files",
					Message = ex
				});
				
				return null;
			}
		}
	}
}