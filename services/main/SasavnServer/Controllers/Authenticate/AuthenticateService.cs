using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.Vkontakte;
using AspNet.Security.OpenId.Steam;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using SasavnServer.Repositories;
using SiteAPI;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SasavnServer.ApiClasses;
using Encryption;
using SasavnServer.Model;
using SasavnServer.Service;

namespace SasavnServer.Controllers.Authenticate
{
	public class RegisterAlreadyException : Exception
    {
        public RegisterAlreadyException(string? message) : base(message)
        {
        }
    }

    public class RegisterException : Exception
    {
        public RegisterException(string? message) : base(message)
        {
        }
    }
    public enum LoginFor
    {
        User, Reseller
    }

    public class LoginArgs
    {
        public string ClientName { get; set; }
        public string Username { get; set; }
        public string? Password { get; set; }
		public bool LoginFromService { get; set; } = false;
        public LoginFor Type { get; set; }
        public string Ip { get; set; }
    }

    public class LoginArgs2
    {
        public User user { get; set; }
        public LoginFor Type { get; set; }
        public string Ip { get; set; }
    }


    public class AuthenticateService
    {

        private readonly string GoogleApiCaptchaKey;
        private readonly IMemoryCache _memoryCache;
        private readonly LoggerService<AuthenticateService> logger;
        private IUserRepository userRepository;
        private static byte[] key = AESGCM.NewKey();

        static readonly string[] Providers = new string[]
        {
            GoogleDefaults.AuthenticationScheme,
            VkontakteAuthenticationDefaults.AuthenticationScheme,
            SteamAuthenticationDefaults.AuthenticationScheme,
            DiscordAuthenticationDefaults.AuthenticationScheme
        };


        public AuthenticateService(
			IMemoryCache memoryCache, 
			IUserRepository userRepository, 
			IConfiguration configuration,
			LoggerService<AuthenticateService> logger)
        {
            _memoryCache = memoryCache;
            this.userRepository = userRepository;
			this.logger = logger;
			GoogleApiCaptchaKey = configuration["captchaKey"];
        }


        public async Task<ErrorCode?> ConfirmRegistration(string uuid)
        {
            if (_memoryCache.TryGetValue(uuid, out AcceptFromEmailModel model))
            {
                return await RegisterUser(model);
            }
            return new ErrorCode(-2, "uuid is invalid");
        }

		
		public class SaltedPassword {
			public string SaltedHashPass {get;set;}
			public string Salt {get;set;}
		}

		static public SaltedPassword HashingPassword(string pass) {

			var hashPass = Cryptography.sha256(pass);

			var salt = Cryptography.sha256(
				Convert.ToBase64String(RandomNumberGenerator.GetBytes(hashPass.Length)
			));

			var saltedPass = Cryptography.sha256(hashPass + salt);

			return new() {
				Salt = salt,
				SaltedHashPass = saltedPass,
			};

		}

		public async Task<User> FindUserWithCredential(string loginOrEmail, string password, bool passIsSalted = false) {

			var nonAccessedUser = await userRepository
				.All()
				.SingleAsync(u => u.Login == loginOrEmail || u.Email == loginOrEmail);

			var hashPass = Cryptography.sha256(password);
			string? saltedPass = Cryptography.sha256(hashPass + nonAccessedUser.Salt);

			return nonAccessedUser.Password == (passIsSalted == true ? password : saltedPass) 
				? nonAccessedUser
				: throw new NullReferenceException();
		}

		//Сгенерено ИИ
		public class CaptchaResponse
		{
			public bool Success { get; set; }
			public float Score { get; set; }
			public string Action { get; set; }
		}		
		private async Task<CaptchaResponse?> VerifyCaptcha(string captcha)
		{
			using var client = new HttpClient();
			var values = new Dictionary<string, string>
			{
				{ "secret", GoogleApiCaptchaKey },
				{ "response", captcha }
			};

			var content = new FormUrlEncodedContent(values);
			var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
			var resultJson = await response.Content.ReadAsStringAsync();

			var captchaResponse = JsonConvert.DeserializeObject<CaptchaResponse>(resultJson);

			logger.Info(new LogParams {
				Title = "VerifyCaptcha",
				Message = captchaResponse,
				Importance = Importance.NotExtremely
			});
			
			return captchaResponse;
		}

        async public Task<ErrorCode?> RegisterUser(AcceptFromEmailModel userResult)
        {
			return await userRepository.BeginTransaction(async (trans) => {
                
                var user = await trans
					.All()
					.FirstOrDefaultAsync(u => u.Login.ToLower() == userResult.Login.ToLower() || u.Email == userResult.Email);

                if (user != null)
                    return new ErrorCode(-1, "User already exist");

				
				var salo = HashingPassword(userResult.Password);

				var newUser = new User()
                {
                    Login = userResult.Login,
                    Email = userResult.Email,
                    Password = salo.SaltedHashPass,
					Salt = salo.Salt,
					Money = 0,
					Roles = new[] { UserRoles.User }
                };

                trans.Add(newUser);

				logger.Info(new LogParams {
					Title = "Create a new user",
					Message = newUser,
					Importance = Importance.Extremely, 
				});	

				return null;

			});
			
        }

        public async Task<ErrorCode?> AcceptFromEmail(AcceptFromEmailModel model, string host, string lang)
        {

			var captchaResult = await VerifyCaptcha(model.Secret);
				
			if (captchaResult == null || captchaResult?.Success != true)
				return new ErrorCode(-4, "captcha error");

			
            var uuid = Guid.NewGuid().ToString();

            _memoryCache.Set(uuid, model,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)));

			//use template generators (maybe Aspose.Html)
            var emailService = new EmailService();

            var labelStyle = "font-size:large;color:white;";
            var link = $"https://{host}/confirm-registration?uuid={uuid}";

			var text = lang == "ru"
				? "Завершение регистрации"
				: "Complete registration";

            var body = $@"
                    <label style={labelStyle}>
                        {text}
                    </label>
                    <a style={"font-size:large;"} href={link}>
                        здесь
                    </a>";

			logger.Info(new LogParams {
				Title = "Try reg a new user",
				Message = model,
				Importance = Importance.Important, 
			});	
			
			await emailService.SendEmailAsync(model.Email, "Registration account", emailService.DefaultBody(body));

			return null;
        }

        public async Task<ErrorCode?> CheckUserHasPassword(User userData)
        {
            var user = await userRepository.FindUserById(userData.Id);

            if (user != null)
            {
                if (user.Password?.Length > 0)
                {
                    return null;
                }
                return new ErrorCode(-2, "user password is not setted");
            }

            return new ErrorCode(-1, "user is null");
        }

        public async Task ResetPassword(User? userData, string uuid, string newPassword)
        {
            if (userData is null)
                throw new Exception();

            if (_memoryCache.TryGetValue(uuid, out string password))
            {
                try
                {
                    var check = await CheckUserHasPassword(userData);
				
					var saltedPass = HashingPassword(newPassword);

					logger.Info(new LogParams {
						Message = "ResetPassword",
						UserId = userData.Id,
						Importance = Importance.Important, 
					});	

					var user = await FindUserWithCredential(userData.Login, password);
					
					ChangePasswordInternal(user, newPassword);
                }
                finally
                {
                    _memoryCache.Remove(uuid);
                }
            }

        }

		private void ChangePasswordInternal(User user, string password, bool inTransaction = false)
		{

			if (inTransaction)
				__Update(userRepository);
			else
				userRepository.BeginTransaction((trans) => __Update(trans));

			void __Update(IUserRepository userRepository)
			{
				logger.Info(new LogParams {
					Message = "Password changing",
					UserId = user.Id,
					Importance = Importance.Very, 
				});	

				var saltedPass = HashingPassword(password);

				user.Password = saltedPass.SaltedHashPass;
				user.Salt = saltedPass.Salt;

				userRepository.Update(user);
			}
		}

		public async Task<string?> ConfirmPassword(User user, string password)
        {
			//if no user then exception
			await FindUserWithCredential(user.Login, password);

            var uuid = Guid.NewGuid().ToString();

            _memoryCache.Set(uuid, password,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(10)));

            return uuid;
        }


        public async Task<LoginResult> LoginFromService(string state, string Ip, string clientName)
        {
            var decryptedState = AESGCM.SimpleDecrypt(
                Base64UrlEncoder.Decode(state),
                key);

            var user = JsonConvert.DeserializeObject<User>(decryptedState);

			logger.Info(new LogParams {
				Message = "Login from service",
				UserId = user.Id,
				Importance = Importance.NotVery, 
			});	


            return await Login(new LoginArgs
            {
                ClientName = clientName,
                Ip = Ip,
                Password = user.Password,
				LoginFromService = true,
                Type = LoginFor.User,
                Username = user.Login
            });
        }


        public string? SerializeAndCryptUser(User? userData)
        {
            if (userData == null) return null;

            var json = userData != null ? JsonConvert.SerializeObject(userData) : "";

            var encrypt = json.Length > 0 ? AESGCM.SimpleEncrypt(json, key) : null;
            return encrypt;
        }

        public async Task<string> RegisterService(
            string authenticationScheme, ClaimsPrincipal principal, 
			string? encryptedUser, bool rebind, string? ReplyTo)
        {
            var claims = principal.Identities
                .Single(c => c.AuthenticationType == authenticationScheme).Claims;

            var (id, login, email) = GetIdLoginEmailFromClaims(authenticationScheme, claims);

            login = Converter.ConvertToLatin(login);

            if (!string.IsNullOrWhiteSpace(encryptedUser))
            {
                var userData = JsonConvert.DeserializeObject<User>(AESGCM.SimpleDecrypt(encryptedUser, key));
                return await AddNewService(
					authenticationScheme: authenticationScheme, 
					rebind: rebind,
					userData: userData, 
					id: id, 
					login: login, 
					replyTo : ReplyTo,
					email: email);
            }

            var socials = await userRepository.FindSocialByServiceId(id);

            if (socials != null)
            {
                var user = await userRepository.FindUserById(socials.UserId);
                string json = JsonConvert.SerializeObject(user);

                var encryptedState = Base64UrlEncoder.Encode(
                    AESGCM.SimpleEncrypt(json, key)
                );

				
				logger.Info(new LogParams {
					Message = "Register new service",
					UserId = user.Id,
				});	


				if(ReplyTo != null)
					return $"/fetch-for-additional-data?replyTo={ReplyTo}&state={encryptedState}";
				else
                	return $"/fetch-for-additional-data?state={encryptedState}";
            }

			return "/login";
        }

        private async Task<string> AddNewService(string authenticationScheme, bool rebind, User? userData, string id, string login, string? replyTo, string? email)
        {
            var redirect = await
                userRepository.BeginTransaction(async (trans) =>
                {

                    var checkCollisionWithOtherUserSocial = await trans.FindSocialByServiceId(id);

                    var existedSocials = await trans.FindSocialByUserId(userData.Id);

					if (checkCollisionWithOtherUserSocial != null && checkCollisionWithOtherUserSocial.UserId != userData.Id)
					{
						return "/register-error";
					}


					var social = existedSocials ?? new Socials
                    {
                        UserId = userData.Id
                    };

                    if (authenticationScheme == GoogleDefaults.AuthenticationScheme)
                        social.GoogleId = id;
                    if (authenticationScheme == VkontakteAuthenticationDefaults.AuthenticationScheme)
                        social.VKID = id;
                    if (authenticationScheme == SteamAuthenticationDefaults.AuthenticationScheme)
                        social.SteamId = id;
                    if (authenticationScheme == DiscordAuthenticationDefaults.AuthenticationScheme)
                        social.DiscordId = id;


					
					logger.Info(new LogParams {
						Message = $"add service: {authenticationScheme}",
						UserId = userData.Id,
					});	


                    if (existedSocials != null)
                        trans.Update(existedSocials);
                    else
                        trans.Add(social);


                    return null;
                });

            if (redirect != null)
                return redirect;

            return await
            userRepository.BeginTransaction(async (trans) =>
            {
                var name = login;

                if (authenticationScheme == GoogleDefaults.AuthenticationScheme)
                {
                    name = email;
                }

                var socialCommon = new SocialUserCommon
                {
                    ServiceGivenName = name ?? login,
                    ServiceId = id,
                    UserId = userData.Id
                };

                var checkCommon = await trans.FindCommonSocialByServiceId(id);

                if (checkCommon != null)
                {
                    if (socialCommon.ServiceId == socialCommon.ServiceId)
                    {
						logger.Warn(new LogParams {
							Title = "Same id while adding new",
							Message = socialCommon,
							UserId = userData.Id,
							Importance = Importance.NotImportant
						});
						

                        checkCommon.ServiceGivenName = socialCommon.ServiceGivenName;
                        checkCommon.UserId = socialCommon.UserId;
                        trans.Update(checkCommon);
						if(replyTo != null)
							return replyTo;
                        else
							return "/profile/integrations";
                    }
                }

                if (rebind)
                {
					logger.Info(new LogParams {
						Title = $"rebind service {id}",
						Message = socialCommon,
						UserId = userData.Id,
						Importance = Importance.NotImportant
					});

                    if (checkCommon == null)
                    {
                        trans.Add(socialCommon);
                    }
                    else
                    {
                        checkCommon.ServiceGivenName = socialCommon.ServiceGivenName;
                        checkCommon.UserId = socialCommon.UserId;
                        trans.Update(checkCommon);
                    }
                }
                else
                {

                    trans.Add(socialCommon);
                }

				if(replyTo != null)
					return replyTo;
				else
					return "/profile/integrations";

            });
        }

        public (string, string, string?) GetIdLoginEmailFromClaims(string authenticationScheme, IEnumerable<Claim> claims)
        {

            var id = claims.First(a => a.Type == ClaimTypes.NameIdentifier).Value;
            if (authenticationScheme == SteamAuthenticationDefaults.AuthenticationScheme)
            {
                var url = new Uri(id);
                var lastId = url.PathAndQuery.Split("/").Last();
                id = lastId;
            }
            var login = claims.First(a => a.Type == ClaimTypes.Name || a.Type == ClaimTypes.GivenName).Value.ToLower();

            var email = claims.FirstOrDefault(a => a.Type == ClaimTypes.Email)?.Value;

            return (id, login, email);
        }

		public class LoginResult
		{
			public string Token { get; set; }
			public string RefreshToken { get; set; }
			public DateTime Expiration { get; set; }
			public ClaimsPrincipal Principal { get; set; }
		}
        public async Task<LoginResult> Login(LoginArgs args)
        {
			logger.Info(new LogParams {
				Title = "Login",
				Message = new {
					args.ClientName,
					args.Ip,
					args.Username
				}
			});

			if(string.IsNullOrEmpty(args.Password) && args.LoginFromService == true) {
				logger.Error(new LogParams {
					Title = "Password is null or empty when try login from service",
					Message = new {
						args.Username
					}
				});
				
				throw new Exception();
			}
			

            var claims = await GetPrincipalFromCreditial(args.Username, args.Password, args.Type, args.LoginFromService);
            var token = CreateToken(claims.Claims.ToList());
            var refreshToken = GenerateRefreshToken();
            var EntityId = -1L;

            foreach (var claim in claims.Claims)
            {
                if (claim.Type == "id")
                {
                    EntityId = long.Parse(claim.Value);
                    break;
                }
            }

			//TODO: timezones
            var tokens = new Tokens
            {
                UserId = EntityId,
                ExpiresIn = DateTime.Now.AddDays(AuthOptions.LIFETIME_REFRESH),
                RefreshToken = refreshToken,

                ClientName = args.ClientName,
                Ip = args.Ip,
                LastActivity = DateTime.Now,
            };

            userRepository.Add(tokens);

            return new LoginResult
            {
                RefreshToken = refreshToken,
                Expiration = tokens.ExpiresIn,
                Principal = claims,
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            };
        }

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

        private async Task<ClaimsPrincipal> GetPrincipalFromCreditial(string username, string? password = null, LoginFor loginFor = LoginFor.User, bool loginedFromService = false)
        {

            List<Claim>? claims = null;

			if(loginFor == LoginFor.Reseller) {

				password = Cryptography.sha256(password!);

                var reseller = await userRepository
					.Resellers()
					.SingleAsync(u => u.Login == username && u.Pass == password);

				claims = Reseller.GetClaims(reseller);
			}
			
            if (loginFor == LoginFor.User)
            {
				var user = password is null 
					? await userRepository
							.All()
							.SingleAsync(u => u.Login == username || u.Email == username)
					: await FindUserWithCredential(username, password, loginedFromService);
					
				claims = User.GetClaims(user);
            }
			
            var id = new ClaimsIdentity(claims!);

            return new ClaimsPrincipal(id);

        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);

        }

        internal async Task RecoveryAccountByEmail(string email, string hostPath)
        {
            await userRepository.BeginTransaction(async (trans) =>
            {
                var emailService = new EmailService();
                var users = trans.All().ToArray();
                var user = await trans.All().SingleAsync(u => u.Email == email);
				
                var uuid = Guid.NewGuid().ToString();


                _memoryCache.Set(uuid, email);

                var labelStyle = "font-size:large;color:white;";
                var link = $"https://{hostPath}/new-password?uuid={uuid}";

				//TODO: translation
                var body = $@"
                    <label style={labelStyle}>
                        Сбросьте пароль
                    </label>
                    <a style={"font-size:large;"} href={link}>
                        по ссылке
                    </a>";

				logger.Info(new LogParams {
					Title = "Try recovery account by email",
					Message = new {
						Email = email
					}
				});
                await emailService.SendEmailAsync(email, "Recovery account", emailService.DefaultBody(body));
            });
        }

        internal async Task RewritePasswordFromRecovery(string uuid, string password)
        {
            await userRepository.BeginTransaction(async (trans) =>
            {
                if (_memoryCache.TryGetValue(uuid, out string email))
                {
                    var user = await trans.All().SingleAsync(u => u.Email == email);

					ChangePasswordInternal(user, password, true);

                }
                else {
					logger.Warn(new LogParams {
						Message = $"uuid not found, email : {email}"
					});	
				}
                
            });
        }
    }

    public static class Converter
    {
        private static readonly Dictionary<char, string> ConvertedLetters = new Dictionary<char, string>
    {
        {'а', "a"},
        {'б', "b"},
        {'в', "v"},
        {'г', "g"},
        {'д', "d"},
        {'е', "e"},
        {'ё', "yo"},
        {'ж', "zh"},
        {'з', "z"},
        {'и', "i"},
        {'й', "j"},
        {'к', "k"},
        {'л', "l"},
        {'м', "m"},
        {'н', "n"},
        {'о', "o"},
        {'п', "p"},
        {'р', "r"},
        {'с', "s"},
        {'т', "t"},
        {'у', "u"},
        {'ф', "f"},
        {'х', "h"},
        {'ц', "c"},
        {'ч', "ch"},
        {'ш', "sh"},
        {'щ', "sch"},
        {'ъ', "j"},
        {'ы', "i"},
        {'ь', "j"},
        {'э', "e"},
        {'ю', "yu"},
        {'я', "ya"},
        {'А', "A"},
        {'Б', "B"},
        {'В', "V"},
        {'Г', "G"},
        {'Д', "D"},
        {'Е', "E"},
        {'Ё', "Yo"},
        {'Ж', "Zh"},
        {'З', "Z"},
        {'И', "I"},
        {'Й', "J"},
        {'К', "K"},
        {'Л', "L"},
        {'М', "M"},
        {'Н', "N"},
        {'О', "O"},
        {'П', "P"},
        {'Р', "R"},
        {'С', "S"},
        {'Т', "T"},
        {'У', "U"},
        {'Ф', "F"},
        {'Х', "H"},
        {'Ц', "C"},
        {'Ч', "Ch"},
        {'Ш', "Sh"},
        {'Щ', "Sch"},
        {'Ъ', "J"},
        {'Ы', "I"},
        {'Ь', "J"},
        {'Э', "E"},
        {'Ю', "Yu"},
        {'Я', "Ya"}
    };

        public static string ConvertToLatin(string source)
        {
            var result = new StringBuilder();
            foreach (var letter in source)
            {
                if (ConvertedLetters.ContainsKey(letter))
                    result.Append(ConvertedLetters[letter]);
                else
                    result.Append(letter.ToString());
            }
            return result.ToString();
        }

    }
}
