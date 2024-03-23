//using Emzi0767.Utilities;

using Microsoft.EntityFrameworkCore;
using SasavnServer.Repositories;
using SasavnServer.usefull;
using Serilog;
using System.IO.Compression;

namespace SasavnServer.Controllers.Assets
{

	internal class ExtractFilesResult
    {
        public string[] Content { get; set; } = Array.Empty<string>();
        public string[] Pictures { get; set; } = Array.Empty<string>();
    }

    public class AssetEditInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public PresentImages[]? SavedImages { get; set; }
        public Content[]? SavedContent { get; set; }
    }

    public class AssetCreateInfo
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
    }

    public class Asset
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }

        public PresentImages[]? PresentImages { get; set; }
        public Content[]? Content { get; set; }

        public string Type { get; set; }
        public DateTime Date { get; set; }
        public int CountInstall { get; set; }

        public int ViewCount { get; set; }

        public override string ToString()
        {
            return @$"Id - {Id}, 
Name - {Name},
Author - {Author}, 
Description - {Description}, 
Type - {Type},
CountInstall - {CountInstall},
Date - {Date},
ImagesPaths - {(PresentImages != null ? PresentImages.Select(image => image.ImagePath).ToString() : "null")},
ImagesPaths - {(Content != null ? Content.Select(content => content.ContentPath).ToString() : "null")}";

        }
    }
    public class AssetsService
    {

        private readonly Serilog.ILogger logger = Log.ForContext<AssetsService>();
        private IAssetsStore assetsStore;
        private PathResolver pathResolver;

        public AssetsService(IConfiguration configuration, IAssetsStore _assetsStore, IHttpContextAccessor httpContextAccessor)
        {
            assetsStore = _assetsStore;
            pathResolver = new PathResolver(
                    new PathString($"{configuration["storePath:path"]}Workshop"),
                    new Uri($"{httpContextAccessor.HttpContext.Request.Scheme}://{configuration["domain"]}/files")
				);
        }

        public int GetAssetsCount()
        {
            try
            {
                return assetsStore.All().Count();
            }
            catch (Exception ex)
            {
                logger.Warning("Пусто, {}", ex.Message);
                return 0;
            }
        }

        private void DeleteFolder(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                logger.Warning("DeleteFolder: {}", ex.Message);
            }
        }

        public async Task DeleteAsset(int id)
        {
            using var trans = assetsStore.BeginTransaction();
            try
            {
                var asset = await assetsStore.All()
                    .Where(a => a.Id == id)
                    .SingleAsync();

                var content = assetsStore.FindByIdContent(asset.Id);

                var pictures = assetsStore.FindByIdImages(asset.Id);

                foreach (var item in content)
                    assetsStore.DeleteContent(item, true);

                foreach (var p in pictures)
                    assetsStore.DeleteImage(p, true);

                assetsStore.Delete(asset, true);

                var path = pathResolver.AbsolutePath($"/{asset.Name}.{asset.Id}");

                await assetsStore.Save();
                await trans.CommitAsync();

                DeleteFolder(path);
            }

            catch (Exception ex)
            {
                await trans.RollbackAsync();
                throw;
            }
        }

        public async Task<Stream> DownloadAsset(int id)
        {
            var asset = await assetsStore.All().Where(a => a.Id == id).SingleAsync();

            var path = pathResolver.AbsolutePath($"/{asset.Name}.{asset.Id}");

            var temp = Path.Combine(Path.GetTempPath(), $@"{DateTime.Now.Ticks}.txt");

            ZipFile.CreateFromDirectory(path, temp);

            var memory = new MemoryStream(await File.ReadAllBytesAsync(temp));

            File.Delete(temp);

            return memory;

        }

        public async Task IncInstallCountOfAsset(int id)
        {
            using var trans = assetsStore.BeginTransaction();
            try
            {
                var asset = await assetsStore.All().Where(a => a.Id == id).SingleAsync();
                asset.CountInstall++;
                assetsStore.Update(asset, true);
                await assetsStore.Save();
                await trans.CommitAsync();

            }
            catch (Exception)
            {
                await trans.RollbackAsync();
                throw;
            }
        }
        public async Task IncViewCountOfAsset(int id)
        {
            using var trans = assetsStore.BeginTransaction();
            try
            {
                var asset = await assetsStore.All().Where(a => a.Id == id).SingleAsync();
                asset.ViewCount++;
                assetsStore.Update(asset, true);
                await assetsStore.Save();
                await trans.CommitAsync();

            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                throw;
            }
        }

        public async Task<Asset> GetAssetById(int id, string host, string scheme)
        {
            var asset = await assetsStore.All().Where(a => a.Id == id).SingleAsync();

            var content = assetsStore.FindByIdContent(asset.Id).ToArray();

            string formatPath(string path)
            {
                return $"{scheme}://{host}/files/Workshop/{path}";
            }

            foreach (var item in content)
            {
                item.ContentPath = formatPath(item.ContentPath.Replace("\\", "/"));

            }
            var images = assetsStore.FindByIdImages(asset.Id).ToArray();

            foreach (var image in images)
            {
                image.ImagePath = formatPath(image.ImagePath.Replace("\\", "/"));
            }


            var asset__ = new Asset
            {
                Id = id,
                Author = asset.Author ?? "ERROR",
                PresentImages = images,
                Content = content,
                Date = asset.Date ?? DateTime.Now,
                Description = asset.Description ?? "",
                Name = asset.Name ?? "ERROR",
                Type = asset.Type ?? "ERROR",
                CountInstall = asset.CountInstall,
                ViewCount = asset.ViewCount
            };

            return asset__;

        }

        public async Task<Asset[]> GetAssets(int offset, int count, string host, string scheme)
        {
			var assets = assetsStore
				.All()
				.AsEnumerable()
				.Take(new Range(offset, offset + count))
				.ToArray();
				
			var assets_ = new List<Asset>(capacity: count);

			foreach(var asset in assets) {

				var _asset = await GetAssetById(asset.Id, host, scheme);
				assets_.Add(_asset);
			}
			
			return assets_.ToArray();		
        }

        async private Task<ExtractFilesResult> ExtractFiles(string assetName, int id, Stream stream)
        {

            string directory = pathResolver.AbsolutePath($"/{assetName}.{id}");

            Directory.CreateDirectory(directory);

            logger.Information($"create directory with path - {directory}");

            using var _stream = File.Create($"{Path.Combine(directory, assetName)}.rar");

            logger.Information($"create rar with path - {Path.Combine(directory, assetName)}.rar");

            await stream.CopyToAsync(_stream);
            _stream.Position = 0;
            using var Z = new ZipArchive(_stream, ZipArchiveMode.Read);

            foreach (var file in Z.Entries)
            {
                logger.Information($"extracting file, cur path - {Path.Combine(directory, file.Name)}");

                file.ExtractToFile(Path.Combine(directory, file.Name), true);
            }

            await _stream.DisposeAsync();

            File.Delete($"{Path.Combine(directory, assetName)}.rar");

            logger.Information($"delete temp rar, path - {Path.Combine(directory, assetName)}.rar");

            var result = new ExtractFilesResult();
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                var ext = Path.GetExtension(file);
                if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
                {
                    logger.Information("file is pic");
                    result.Pictures = result.Pictures.Append(file).ToArray();
                }
                if (ext == ".lua" || ext == ".xml" || ext == ".json")
                {
                    logger.Information("file is dick");
                    result.Content = result.Content.Append(file).ToArray();
                }
            }

            stream.Position = 0;
            return result;
        }

        async public Task CreateAsset(AssetCreateInfo asset, Stream stream)
        {
            using var trans = assetsStore.BeginTransaction();
            try
            {

                var Assets = assetsStore.All();

                var assetInfo = new AssetInfo
                {
                    Name = asset.Name,
                    Description = asset.Description,
                    Date = DateTime.Now,
                    Author = asset.Author,
                    CountInstall = 0
                };

                assetsStore.Add(assetInfo, true);

                await assetsStore.Save();

                var _asset = await assetsStore.All().Where(a => a == assetInfo).SingleAsync();

                var result = await ExtractFiles(asset.Name, _asset.Id, stream);

                foreach (var image in result.Pictures)
                {
					
                    var _image = Path.GetRelativePath(pathResolver.AbsolutePath(), image);

                    logger.Information($"lol, image path create - {_image}");

                    var images = new PresentImages
                    {
                        ImagePath = _image,
                        ImageId = _asset.Id
                    };

                    assetsStore.Add(images, true);
                }

                foreach (var content in result.Content)
                {
                    var _content = Path.GetRelativePath(pathResolver.AbsolutePath(), content);

                    var contents = new Content
                    {
                        ContentPath = _content,
                        ContentId = _asset.Id
                    };
                    assetsStore.Add(contents, true);
                }
                await assetsStore.Save();

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        private void RenameFolder(string oldFolderName, string newFolderName)
        {
            if (string.IsNullOrEmpty(oldFolderName))
                throw new NullReferenceException();

            if (string.IsNullOrEmpty(newFolderName))
                throw new NullReferenceException();


            var path_ = pathResolver.AbsolutePath($"/{oldFolderName}");

            logger.Information($"lol, path : {path_}");

            if (Directory.Exists(path_))
            {
            	var newPath = pathResolver.AbsolutePath($"/{newFolderName}");
                logger.Information($"lol, path : {newPath}");
                if (path_ != newPath)
                {
                    Directory.Move(path_, newPath);
                    logger.Information("lol, directory was moved");
                }
                else
                {
                    logger.Information("nothing to move");
                }
            }

        }
        private async Task UpdateImagesAndContent(ExtractFilesResult? result, PresentImages[] savedImages, Content[] savedContent, AssetEditInfo asset)
        {
            assetsStore.DeleteContent(asset.Id, true);
            assetsStore.DeleteImage(asset.Id, true);

            string directory = pathResolver.AbsolutePath($"/{asset.Name}.${asset.Id}");

            logger.Information($"lol, path : {directory}");
            var filesToSave = new List<string>();

            foreach (var file in Directory.EnumerateFiles(directory))
            {
				
            	var pathToFile = pathResolver.AbsolutePath($"/{file}");
                logger.Information($"lol, path : {pathToFile}");

                var pathsImages = savedImages.Select(i => Path.GetRelativePath(pathResolver.AbsoluteUri().ToString(), i.ImagePath));
                var pathsContent = savedContent.Select(c => Path.GetRelativePath(pathResolver.AbsoluteUri().ToString(), c.ContentPath));

                if (pathsImages.Contains(pathToFile))
                {
                    logger.Information($"lol, path : {pathToFile}");
                    filesToSave.Add(pathToFile);
                }

                if (pathsContent.Contains(pathToFile))
                {
                    logger.Information($"lol, path : {pathToFile}");
                    filesToSave.Add(pathToFile);
                }
            }

            var files = Directory.EnumerateFiles(directory).Select(path => Path.GetRelativePath(pathResolver.AbsolutePath(), path));
            var lol = files.Except(filesToSave).Select(path => Path.Combine(pathResolver.AbsolutePath(), path));


            foreach (var file in lol)
            {
                File.Delete(file);
            }

            if (filesToSave.Count == 0)
            {
                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    if (result != null)
                    {
                        if (!result.Content.Contains(file) && !result.Pictures.Contains(file))
                            File.Delete(file);
                    }
                    else
                    {
                        File.Delete(file);
                    }

                }
            }

            foreach (var image in savedImages)
            {
                logger.Information($"lol, try - {Path.GetRelativePath(pathResolver.AbsoluteUri().ToString(), image.ImagePath)}");

                var imagePath = Path.GetRelativePath(pathResolver.AbsoluteUri().ToString(), image.ImagePath);
                var images = new PresentImages
                {
                    ImagePath = imagePath,
                    ImageId = asset.Id
                };
                assetsStore.Add(images, true);
            }


            foreach (var content in savedContent)
            {
                var contentPath = Path.GetRelativePath(pathResolver.AbsoluteUri().ToString(), content.ContentPath);
                var contents = new Content
                {
                    ContentPath = contentPath,
                    ContentId = asset.Id
                };
                assetsStore.Add(contents, true);
            }

            if (result != null)
            {
                logger.Information($"lol, image path create - {"result not null"}");

                foreach (var image in result.Pictures)
                {
                    var images = new PresentImages
                    {
                        ImagePath = Path.GetRelativePath(pathResolver.AbsolutePath(), image),
                        ImageId = asset.Id
                    };
                    //  logger.Log(LogLevel.Information, "image create with - {}", images.ToDictionary());
                    assetsStore.Add(images, true);
                }

                foreach (var content in result.Content)
                {
                    var contents = new Content
                    {
                        ContentPath = Path.GetRelativePath(pathResolver.AbsolutePath(), content),
                        ContentId = asset.Id
                    };
                    assetsStore.Add(contents, true);
                }
            }

            await assetsStore.Save();
        }

        async public Task EditAsset(AssetEditInfo asset, Stream? stream)
        {
            using var trans = assetsStore.BeginTransaction();
            var folderNameIfExcept = "";
            try
            {
                ExtractFilesResult? result = null;


                var _asset = await assetsStore.All().Where(a => a.Id == asset.Id).SingleAsync();

                if (stream != null)
                    result = await ExtractFiles(asset.Name, _asset.Id, stream);

                folderNameIfExcept = $"{_asset.Name}.{_asset.Id}";

                RenameFolder($"{_asset.Name}.{_asset.Id}", $"{asset.Name}.{asset.Id}");


                await UpdateImagesAndContent(result, asset.SavedImages ?? Array.Empty<PresentImages>()
                                                   , asset.SavedContent ?? Array.Empty<Content>(), asset);

                _asset.Author = asset.Author;
                _asset.Name = asset.Name;
                _asset.Description = asset.Description;

                assetsStore.Update(_asset, true);

                await assetsStore.Save();
                trans.Commit();

            }
            catch (Exception ex)
            {
                logger.Error($"lol, image path create - {ex.Message}");
                try
                {
                    RenameFolder($"{asset.Name}.{asset.Id}", folderNameIfExcept);
                }
                catch (Exception) { logger.Error("Bleat, can't rename, some prblms"); }

                trans.Rollback();
                throw;
            }
        }
    }
}
