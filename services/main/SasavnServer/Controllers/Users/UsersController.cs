using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using SasavnServer.ApiClasses;
using SasavnServer.Repositories;
using SasavnServer.Controllers.ChangeLogs;
using SasavnServer.Service;

namespace SasavnServer.Controllers.Users
{
	[Route("api/[controller]")]
    [ApiController]
	[Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository userRepository;
        private readonly LoggerService<UsersController> logger;
        private readonly UsersService usersService;
        private readonly ChangeLogsService changeLogsService;

        public UsersController(
			IUserRepository userRepository,
            UsersService usersService,
            ChangeLogsService changeLogsService,
			LoggerService<UsersController> logger
			)
        {
			this.logger = logger;
            this.userRepository = userRepository;
            this.usersService = usersService;
            this.changeLogsService = changeLogsService;
        }

        [Authorize]
        [HttpGet("getInfoAboutPurchasedProducts")]
        public async Task<Subscription[]> GetInfoAboutPurchasedProducts()
        {
            var user = HttpContext.User.GetUserData();
            var key = await userRepository
				.Subscriptions()
				.Where(k => k.UserId == user.Id)
				.ToArrayAsync();

            return key;
        }

		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorCode))]
        [HttpPost("sendTokenToLauncher")]
        public async Task<ActionResult> SendTokenToLauncher([FromBody] SendTokenToLauncherModel model)
        {
			logger.Info(new LogParams {
				Title = "Send token to launcher",
				Message = model,
				Importance = Importance.NotImportant
			});

			await usersService.SendAuthDataToLauncher(
				uuid: model.Uuid, 
				accessToken: model.AccessToken, 
				refreshToken: model.RefreshToken, 
				expiration: model.Expiration,
				password: model.Password);

			return Ok();

        }


		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetUserProfileImageResult))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorCode))]
        [HttpPost("getUserProfileImage")]
        public ActionResult<GetUserProfileImageResult> GetUserProfileImage([FromBody] GetUserProfileImageModel model)
        {   
            var linkToImage = usersService.GetUserProfileImage(model.Username);

            if (linkToImage == null)
            	return BadRequest(new ErrorCode(-1, "Image not found"));

			return Ok(new GetUserProfileImageResult
			{
				ImageUrl = linkToImage.AbsolutePath
			});
        }


        [Authorize]
        [HttpPost("LoadProfileImage")]
        async public Task<ActionResult<string>> LoadProfileImage([FromForm] LoadProfileImageModel model)
        {
            var user = HttpContext.User.GetUserData();

            var result = await usersService.LoadProfileImage(model.File, user);

            if (result != null)
                return BadRequest(result);

            return Ok(usersService.GetUserProfileImage(user.Login)!.LocalPath);
        }

		
   	 	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
   	 	[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorCode))]
        [Authorize]
        [HttpPost("ActivateKey")]
        async public Task<IActionResult> ActivateKey([FromBody] ActivateKeyModel model)
        {
            var error = await usersService.ActivateKey(model.Key, HttpContext.User.GetUserData());

            if (error != null)
                return BadRequest(error);

            return Ok();

        }

    }
}
