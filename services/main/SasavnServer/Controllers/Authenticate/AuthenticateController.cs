using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SasavnServer.ApiClasses;
using SiteAPI;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;
using SasavnServer.Repositories;
using AspNet.Security.OpenId.Steam;
using AspNet.Security.OAuth.Vkontakte;
using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authorization;
using SasavnServer.Model;
using NSwag.Annotations;
using System.Globalization;
using SasavnServer.Service;

namespace SasavnServer.Controllers.Authenticate
{

	class LoginException : Exception { }

    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticateController : Controller
    {
		private readonly LoggerService<AuthenticateController> logger;

        private readonly IUserRepository _userRepository;

        private readonly AuthenticateService _authenticationService;

        public AuthenticateController(
			IUserRepository userRepository, 
			AuthenticateService authenticateService,
			LoggerService<AuthenticateController> logger)
        {
            _userRepository = userRepository;
            _authenticationService = authenticateService;
			this.logger = logger;
        }

		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorCode))]
        [HttpPost("ConfirmRegistration")]
        public async Task<ActionResult> ConfirmRegistration([FromBody] ConfirmRegistrationModel model)
        {
            var errorCode = await _authenticationService.ConfirmRegistration(model.Uuid);

            if (errorCode != null)
                return BadRequest(errorCode);

            return Ok();
        }


		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorCode))]
        [HttpPost("AcceptFromEmail")]
        public async Task<ActionResult> AcceptFromEmail([FromBody] AcceptFromEmailModel model)
        {
            var error = await _authenticationService.AcceptFromEmail(
				model: model, 
				host: HttpContext.Request.Host.Value, 
				lang: CultureInfo.CurrentCulture.Name);

			if(error != null) {
				return BadRequest(error);
			}
			
			return Ok();
        }


		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [HttpPost("rewritePasswordFromRecovery")]
        public async Task<ActionResult> RewritePasswordFromRecovery([FromBody] RewritePasswordFromRecoveryModel model)
        {
            await _authenticationService.RewritePasswordFromRecovery(model.Uuid, model.Password);

			return Ok();
        }

		
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorCode))]
        [HttpPost("recoveryAccountByEmail")]
        public async Task<ActionResult> RecoveryAccountByEmail([FromBody] RecoveryAccountByEmailModel model)
        {
            await _authenticationService.RecoveryAccountByEmail(model.Email, HttpContext.Request.Host.Value);

			return Ok();
        }


        [Authorize]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorCode))]
        [HttpPost("ResetPassword")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            var userData = HttpContext.User.GetUserData();

            await _authenticationService.ResetPassword(userData, model.Uuid, model.Password);

			return Ok();

        }

        [Authorize]
        [HttpPost("ConfirmPassword")]
        public async Task<ActionResult<string>> ConfirmPassword([FromBody] ConfirmPasswordModel model)
        {
            var userData = HttpContext.User.GetUserData()!;

            return Ok(
                await _authenticationService.ConfirmPassword(userData, model.Password)
                );
        }

		[AllowAnonymous]
        [Authorize]
        [HttpPost("discordLogin")]
        [HttpPost("googleLogin")]
        [HttpPost("vkLogin")]
        [HttpPost("steamLogin")]
        public async Task<ActionResult<string>> DefaultLogin([FromBody] DefaultLoginModel model)
        {
			var methodUri = new Uri(HttpContext.Request.Path);

            var authenticationScheme = methodUri.Segments.Last() switch
            {
                "googleLogin" => GoogleDefaults.AuthenticationScheme,
                "discordLogin" => DiscordAuthenticationDefaults.AuthenticationScheme,
                "vkLogin" => VkontakteAuthenticationDefaults.AuthenticationScheme,
                "steamLogin" => SteamAuthenticationDefaults.AuthenticationScheme,
                _ => throw new Exception("no way")
            };

            var redirectUri = authenticationScheme switch
            {
                //Хз, но почему-то кому-то нужна эта чёрточка, а кому-то нет, лень разбираться.
                GoogleDefaults.AuthenticationScheme => "/api/authenticate/google-response/",
                DiscordAuthenticationDefaults.AuthenticationScheme => "/api/authenticate/discordResponse",
                VkontakteAuthenticationDefaults.AuthenticationScheme => "/api/authenticate/vkResponse/",
                SteamAuthenticationDefaults.AuthenticationScheme => "/api/authenticate/steamResponse/",
                _ => throw new Exception("no way")
            };


            logger.Info(new LogParams {
				Message = $"try login : {methodUri.Query}",
				Importance = Importance.NotExtremely
			});

            return Ok(await DefaultLogin(
                new AuthenticationProperties(
                    items: new Dictionary<string, string?> {
                        { "user", _authenticationService.SerializeAndCryptUser(HttpContext.User.GetUserData()) },
                        { "rebind", model.Rebind.ToString() },
						{ "ReplyTo", model.ReplyTo },
                    }
                )
                {
                    RedirectUri = redirectUri
                },

                authenticationScheme
            ));
        }

		
		[ApiExplorerSettings(IgnoreApi=true)]
		[OpenApiIgnore]
        [HttpGet("discordResponse/")]
        [HttpGet("vkResponse")]
        [HttpGet("steamResponse")]
        [HttpGet("google-response")]
        public async Task<IActionResult> _Response()
        {
			var methodUri = new Uri(HttpContext.Request.Path);

            string? authenticationScheme = methodUri.Segments.Last().TrimEnd('/') switch
            {
                "google-response" => GoogleDefaults.AuthenticationScheme,
                "discordResponse" => DiscordAuthenticationDefaults.AuthenticationScheme,
                "vkResponse" => VkontakteAuthenticationDefaults.AuthenticationScheme,
                "steamResponse" => SteamAuthenticationDefaults.AuthenticationScheme,
                _ => throw new Exception("no way")
            };

            return await DefaultResponse(authenticationScheme);
        }

		[ApiExplorerSettings(IgnoreApi=true)]
		[OpenApiIgnore]
        public async Task<string> DefaultLogin(AuthenticationProperties properties, string scheme)
        {
            await HttpContext.ChallengeAsync(scheme, properties);
            string location = HttpContext.Response.Headers.Location!;
            HttpContext.Response.Headers.Location = "";
            return location;
        }

		[ApiExplorerSettings(IgnoreApi=true)]
		[OpenApiIgnore]
        public async Task<IActionResult> DefaultResponse(string authenticationScheme)
        {

            var result = await HttpContext.AuthenticateAsync(authenticationScheme);
            _ = bool.TryParse(result.Properties?.Items["rebind"], out var rebind);

            try
            {

				var replyTo = result.Properties?.Items["ReplyTo"];

                var url = await _authenticationService.
                    RegisterService(
						authenticationScheme: authenticationScheme, 
						principal: result.Principal, 
						encryptedUser: result.Properties?.Items["user"], 
						rebind: rebind,
						ReplyTo: replyTo);

                return Redirect(url);
            }
            catch
            {
                if (result.Properties?.Items["user"] != null)
                    return Redirect("/profile/integrations");
                else
                    return Redirect("/login");
            }
        }

        [HttpGet("AlwaysThrow")]
		public void AlwaysThrow() {
			throw new Exception("Always throw");
		}

        [Authorize]
        [HttpGet("GetSocialCommons")]
        public async Task<SocialUserCommon[]> GetSocialCommons()
        {
            var user = HttpContext.User.GetUserData();
           	var socials = await _userRepository.FindAllCommonSocialByUserId(user.Id);

			logger.Info(new LogParams {
				Title = "GetSocials",
				Message = socials,
				Importance = Importance.NotExtremely
			});

            return socials;
        }


   	 	[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [Authorize]
        [HttpPost("removeSocialId/{type}")]
        public async Task<IActionResult> RemoveSocialId(string type)
        {
            var user = HttpContext.User.GetUserData();
            var social = await _userRepository.FindSocialByUserId(user.Id);

            var id = type switch
            {
                GoogleDefaults.AuthenticationScheme => social!.GoogleId,
                VkontakteAuthenticationDefaults.AuthenticationScheme => social!.VKID,
                SteamAuthenticationDefaults.AuthenticationScheme => social!.SteamId,
                DiscordAuthenticationDefaults.AuthenticationScheme => social!.DiscordId,
                _ => throw new NotImplementedException(),
            };


            if (social != null)
            {

                if (type == GoogleDefaults.AuthenticationScheme)
                    social.GoogleId = "";
                if (type == VkontakteAuthenticationDefaults.AuthenticationScheme)
                    social.VKID = "";
                if (type == SteamAuthenticationDefaults.AuthenticationScheme)
                    social.SteamId = "";
                if (type == DiscordAuthenticationDefaults.AuthenticationScheme)
                    social.DiscordId = "";


                await _userRepository.BeginTransaction(async (trans) =>
                {
                    trans.Update(social);
                    await trans.RemoveCommonSocialByServiceId(id);
                });

				logger.Info(new LogParams {
					Message = $"remove SocialId {id}",
					UserId = user.Id,
					Importance = Importance.NotExtremely
				});

                return Ok();
            }

            return BadRequest(new ErrorCode(-1, "social is null"));

        }

        [Authorize]
        [HttpGet("GetSocial")]
        public async Task<Socials> GetSocial()
        {
            var user = HttpContext.User.GetUserData();
            var social = await _userRepository.FindSocialByUserId(user.Id);

            if (social != null)
            {
                return social;
            }
            else
            {
                var newSocial = new Socials
                {
                    UserId = user.Id
                };
                _userRepository.Add(newSocial);

                return newSocial;
            }

        }

		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [Authorize]
        [HttpPost("closeSession")]
        public async Task<ActionResult> CloseSession([FromBody] CloseSessionModel model)
        {
            await
            _userRepository.BeginTransaction(async (trans) =>
            {
                var token = await trans.FindTokensByRefreshToken(model.RefreshToken);

                trans.Remove(token);

            });

            return Ok();
        }

		[Authorize]
        [HttpGet("getSessionsInfo")]
        public async Task<ActionResult<Tokens[]>> GetSessionsInfo()
        {
            var user = HttpContext.User.GetUserData();
            var userId = user.Id;

            var tokens = await _userRepository.FindTokensByUserId(userId);

            if (tokens == null)
                return BadRequest(new ErrorCode(-1, "tokens is null"));
            
            return Ok(tokens);
        }

		
        [HttpPost("LoginFromService")]
        public async Task<ActionResult<LoginFromServiceResult>> LoginFromService([FromBody] LoginFromServiceModel model)
        {
            try
            {
                var Ip = Request.HttpContext.Request.Headers["X-Real-IP"];
                var result = await _authenticationService.LoginFromService(model.State, Ip, model.ClientName);

				var maxAge = result.Expiration - DateTime.Now;

				Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions {
					MaxAge = maxAge, HttpOnly = true, Secure = true 
				});

                return Ok(new LoginFromServiceResult
                {
                    Token = result.Token,
                    RefreshToken = result.RefreshToken,
                    Expiration = result.Expiration
                });
            }
            catch (RegisterException)
            {
                return BadRequest(new ErrorCode(-1, "Failed to register by service"));
            }
        }

        [HttpPost("Login")]
		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginResult))]
		[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorCode))]
        public async Task<ActionResult<LoginResult>> Login([FromBody] LoginModel model)
        {
            try
            {
                var Ip = Request.HttpContext.Request.Headers["X-Real-IP"];

                var result = await _authenticationService.Login(new LoginArgs
                {
                    ClientName = model.ClientName,
                    Ip = Ip,
                    Password = model.Password,
                    Type = model.Type,
                    Username = model.Login,
                });

				var cookieKey = model.Type == LoginFor.User 
					? "refreshToken"
					: "refreshToken_r";

				Response.Cookies.Append(cookieKey, value: result.RefreshToken, 
					new CookieOptions {
						MaxAge = result.Expiration - DateTime.Now, 
						HttpOnly = true, 
						Secure = true, 
						SameSite = SameSiteMode.Lax
				});

				return Ok(new LoginResult
				{
					Token = result.Token,
					RefreshToken = result.RefreshToken,
					Expiration = result.Expiration
				});

            }
            catch (LoginException)
            {
                logger.Error(new LogParams {
					Message = $"Failed login, incorrect creditials, login : {model.Login}"
				});
                return BadRequest(new ErrorCode(-1, "pass or login incorrect"));
            }
            catch(Exception ex )
            {
				logger.Error(new LogParams {
					Title = $"Failed login, exception",
					Message = new {
						model,
						ex
					}
				});

                return BadRequest(new ErrorCode(-1, "pass or login incorrect"));
            }
        }


		[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(object))]
        [HttpPost("logout")]
        public async Task<ActionResult> Logout()
        {

            foreach (var cookie in HttpContext.Request.Cookies)
            {
                Response.Cookies.Delete(cookie.Key);
            }

            var prop = new AuthenticationProperties()
            {
                RedirectUri = "/"
            };

            await HttpContext.SignOutAsync("Cookies", prop);

            return Ok();
        }


        [HttpPost("refreshToken")]
        public async Task<ActionResult<RefreshTokenResult>> RefreshToken([FromBody] RefreshTokenModel model)
        {

            var userToken = await _userRepository.FindTokensByRefreshToken(model.RefreshToken);
            var user = await _userRepository.FindUserByRefreshToken(model.RefreshToken);

            if (userToken.ExpiresIn <= DateTime.Now)
            {
                //TODO: recreate token?
                return BadRequest("Invalid access token or refresh token");
            }

            userToken.LastActivity = DateTime.Now;
            JwtSecurityToken? newAccessToken;

            if (model.IsReseller != null && model.IsReseller == true)
            {
                var reseller = await _userRepository.FindResellerByRefreshToken(model.RefreshToken);
                newAccessToken = CreateToken(Reseller.GetClaims(reseller));
            }
            else
                newAccessToken = CreateToken(Repositories.User.GetClaims(user));

            _userRepository.Update(userToken);

			return Ok(new RefreshTokenResult
			{
				AccessToken =  new JwtSecurityTokenHandler().WriteToken(newAccessToken)
			});
        }


		[OpenApiIgnore]
        private JwtSecurityToken CreateToken(List<Claim> authClaims)
        {
            var authSigningKey = AuthOptions.GetSymmetricSecurityKey();


            var token = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                expires: DateTime.Now.AddMinutes(AuthOptions.LIFETIME),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

            return token;

        }
	}
}
