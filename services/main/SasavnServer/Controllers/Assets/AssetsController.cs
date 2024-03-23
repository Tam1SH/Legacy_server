
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SasavnServer.ApiClasses;
using SasavnServer.Repositories;

namespace SasavnServer.Controllers.Assets
{
	[Route("api/[controller]")]
    [ApiController]
    public class AssetsController : ControllerBase
    {
        private AssetsService assetsService;
		
        public AssetsController(AssetsService _assetsService)
        {
            assetsService = _assetsService;

        }

        [HttpGet("getAssetsCount")]
        public int GetAssetsCount()
        {
            return assetsService.GetAssetsCount();
        }

   	 	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
		[Authorize]
        [HttpPost("createAsset")]
        public async Task<IActionResult> CreateAsset([FromForm] CreateAssetModel model)
        {
			try
			{
				using var stream = model.Zip.OpenReadStream();

				var user = HttpContext.User.GetUserData()!;

				var asset = new AssetCreateInfo
				{
					Author = user.Login,
					Description = model.Description,
					Name = model.Name,
				};
				
				await assetsService.CreateAsset(asset, stream);

				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest(new ErrorCode(-1, ex.Message));
			}
        }

   	 	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
		[Authorize]
		[ClaimRequirement(ClaimTypes.Role, "Admin")]
        [HttpDelete("deleteAsset/{id}")]
        public async Task DeleteAsset(int id)
        {
			await assetsService.DeleteAsset(id);
        }

        [HttpGet("installAsset/{id}")]
        public async Task<FileStreamResult> InstallAsset(int id)
        {
			var stream = await assetsService.DownloadAsset(id);
			await assetsService.IncInstallCountOfAsset(id);

			var contentType = "APPLICATION/octet-stream";
			return File(stream, contentType, "Asset");
			
        }

        [Authorize]
        [HttpGet("installInc/{id}")]
        public async Task InstallInc(int id)
        {
			await assetsService.IncInstallCountOfAsset(id);
        }

        [HttpGet("viewInc/{id}")]
        public async Task ViewInc(int id)
        {
			await assetsService.IncViewCountOfAsset(id);
        }

		[Authorize(Roles = "Admin,Helper")]
        [HttpPost("editAsset")]
        public async Task EditAsset([FromForm] EditAssetModel model)
        {
			
			var asset = new AssetEditInfo
			{
				Id = model.Id,
				Author = model.Author,
				Description = model.Description,
				Name = model.Name,
				SavedContent = JsonConvert.DeserializeObject<Content[]>(model.SavedContents),
				SavedImages = JsonConvert.DeserializeObject<PresentImages[]>(model.SavedPictures)
			};

			if (Request.Form.Files != null && Request.Form.Files.Count != 0)
			{
				using var stream = Request.Form.Files[0].OpenReadStream();
				await assetsService.EditAsset(asset, stream);
			}
			else
				await assetsService.EditAsset(asset, null);
				
        }


        [HttpGet("getAssetById/{id}")]
        public async Task<Asset> GetAssetById(int id)
        {
            var host = Request.Headers["X-Forwarded-Server"];
            
			return await assetsService.GetAssetById(id, host, Request.Scheme);
        }

        [HttpPost("getAssets")]
        public async Task<ActionResult<Asset[]>> GetAssets([FromBody] GetAssetsModel model)
        {

            var host = Request.Headers["X-Forwarded-Server"];

			var scheme = Request.Scheme;
			
			var assetInfos = await assetsService.GetAssets(model.Offset, model.Count, host, scheme);

			return Ok(assetInfos);

        }
    }
}
