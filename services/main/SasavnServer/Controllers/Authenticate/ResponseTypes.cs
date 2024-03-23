namespace SasavnServer.Controllers.Authenticate
{
	public class RefreshTokenResult
	{
		public string AccessToken { get; set; }
	};
	
	public class RefreshTokenModel
	{
		public string? RefreshToken  { get; set; }
		public bool? IsReseller { get; set; }
	};

	public class LoginResult 
	{ 
		public string Token { get; set; }
		public string RefreshToken { get; set; }
		public DateTime Expiration { get; set; }
	}

	public class LoginFromServiceResult 
	{ 
		public string Token { get; set; }
		public string RefreshToken { get; set; }
		public DateTime Expiration { get; set; }
	}

}