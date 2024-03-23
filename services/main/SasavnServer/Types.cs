using System.Security.Claims;

namespace SasavnServer.Model
{
	
    public class NotUniqueException : Exception
    {
        public NotUniqueException(string message) : base(message)
        {
        }
    }


    public class Reseller
    {
        public const long MagicOffset = 10_000_000_000;
        public long Id { get; set; }
        public string Login { get; set; }
        public string Pass { get; set; }
        public int AvailableKeys { get; set; }
        public int ActivatedKeys { get; set; }
        public int AvailableHwidKeys { get; set; }
        public int ActivatedHwidKeys { get; set; }
        public int Official { get; set; }
        public string AttachedTo { get; set; }

		static public List<Claim> GetClaims(Reseller reseller) {

			return new List<Claim>
			{
				new(ClaimTypes.Role, "Reseller"),
				new(ClaimTypes.Role, "User"),
				new Claim("id", (reseller.Id + MagicOffset).ToString()),
				new Claim("Login", reseller.Login),
				new Claim("AvailableKeys", reseller.AvailableKeys.ToString()),
				new Claim("ActivatedKeys", reseller.ActivatedKeys.ToString()),
				new Claim("AvailableHwidKeys", reseller.AvailableHwidKeys.ToString()),
				new Claim("ActivatedHwidKeys", reseller.ActivatedHwidKeys.ToString()),
				new Claim("Official", reseller.Official.ToString()),
				new Claim("AttachedTo", reseller.AttachedTo.ToString())
			};
		}
        static public Reseller FromIdentity(ClaimsIdentity identity)
        {
            return new()
            {
                Id = long.Parse(identity.FindFirst("id")!.Value) - Reseller.MagicOffset,
                Login = identity.FindFirst("Login")!.Value,
                AvailableKeys = int.Parse(identity.FindFirst("AvailableKeys")!.Value),
                ActivatedKeys = int.Parse(identity.FindFirst("ActivatedKeys")!.Value),
                AvailableHwidKeys = int.Parse(identity.FindFirst("AvailableHwidKeys")!.Value),
                ActivatedHwidKeys = int.Parse(identity.FindFirst("ActivatedHwidKeys")!.Value),
                Official = int.Parse(identity.FindFirst("Official")!.Value),
                AttachedTo = identity.FindFirst("AttachedTo")!.Value
            };
        }
    }

}
