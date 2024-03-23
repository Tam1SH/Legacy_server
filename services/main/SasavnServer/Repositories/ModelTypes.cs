using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;


namespace SasavnServer.Repositories
{

    public enum UserRoles { User, Admin, Helper, Reseller }

	public class LegacyUser {
        public long Id { get; set; }
        public string Login { get; set; }
        public string Email { get; set; }
        public long Money { get; set; }
		public long Role {get;set;}
		public string Password {get; set;}
		public string Hwid { get; set; }
		public DateTime TimeUpdate { get; set; }
		public DateTime HwidResetDate { get; set; }
		public string AdminMessage { get; set; }
		public string AuthToken {get;set;}
	}

    public class User
    {
		public long Id { get; set; }
        public string Login { get; set; }
        public string Email { get; set; }
		public string Salt {get;set;}
        public long Money { get; set; }
        public UserRoles[] Roles { get; set; }

        [Column("Password")]
        public string? Password
        {
            get
            {
                return TransformedFromToken == true ? null : _password;
            }

            set
            {
                if (TransformedFromToken == true)
                    throw new Exception("don't touch me dear, cuz im TransformedFromToken");

                _password = value;
            }
        }

        private string? _password;

        [NotMapped]
        public bool TransformedFromToken { get; set; } = false;

		public static List<Claim> GetClaims(User user)
        {

            var claims = new List<Claim>
            {
                    new ("id", user.Id.ToString()),
                    new (ClaimTypes.Email, user.Email),
                    new ("login", user.Login)
            };

            foreach (var claim in user.Roles.Select(r => new Claim(ClaimTypes.Role, r.ToString())))
            {
                claims.Add(claim);
            }

            return claims;
        }

        public static User? FromIdentity(ClaimsIdentity claimsIdentity)
        {
		
			var Email = claimsIdentity.FindFirst(ClaimTypes.Email)
							//TODO: легаси
							?? claimsIdentity.FindFirst("email");

			var Id =  claimsIdentity.FindFirst("id");
			var Login = claimsIdentity.FindFirst("login");

            return new()
            {
                Email = Email!.Value,
                Id = long.Parse(Id!.Value),
                Login = Login!.Value,
                Password = null,
                TransformedFromToken = true,
                Roles = claimsIdentity
					.FindAll(c => c.Type == ClaimTypes.Role)
					.Select(c => (UserRoles)Enum.Parse(typeof(UserRoles), c.Value))
					.ToArray()
            };

        }
    }

    public class GameUser
    {
        [Key]
        public long UserId { get; set; }
        public string HWID { get; set; }
		public string AuthToken { get; set; }
        public DateTime HwidResetDate { get; set; }
    }


    public class Transaction
    {
        public long Id { get; set; }
        public string Status { get; set; }
        public long Value { get; set; }
        public long UserId { get; set; }
        public string TSHash { get; set; }
        public DateTime Date { get; set; }
    }


    public class KeyCodes
    {
        public int Id { get; set; }
        public string KeyName { get; set; }
        public string Reseller { get; set; }
        public string Activatedby { get; set; }
        public int Permissions { get; set; }
        public int Activated { get; set; }
        public DateTime ActivationDate { get; set; }
    }

    public class Updates
    {
        public long Id { get; set; }
        public string Version { get; set; }
        public DateTime CreationDate { get; set; }
    }

    public class Tokens
    {
        [Key]
        public string RefreshToken { get; set; }
        public long UserId { get; set; }
        public DateTime ExpiresIn { get; set; }

        public string ClientName { get; set; }
        public string Ip { get; set; }
        public DateTime LastActivity { get; set; }

    }

    public class Subscription
    {
		public enum GameIdentificator { GTA5, CS2 }
        public long Id { get; set; }
        public long UserId { get; set; }
        public DateTime ActivationDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public DateTime TimeUpdate { get; set; }
        public string AdminMessage { get; set; }
        public GameIdentificator GameId { get; set; }

    }

    public class Stats
    {
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public long OnlineCount { get; set; }
        public long TotalLogins { get; set; }
        public long TotalUsedTime { get; set; }

    }

    public class Socials
    {
        [Key]
        public long UserId { get; set; }
        public string GoogleId { get; set; } = "";
        public string VKID { get; set; } = "";
        public string SteamId { get; set; } = "";
        public string DiscordId { get; set; } = "";
    }

    public class SocialUserCommon
    {
        [Key]
        public string ServiceId { get; set; }
        public long UserId { get; set; }
        public string ServiceGivenName { get; set; }
    }

}
