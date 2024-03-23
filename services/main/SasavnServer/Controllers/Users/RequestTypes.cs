namespace SasavnServer.Controllers.Users
{
	public class SendTokenToLauncherModel {
		public string Uuid { get; set; }
		public string AccessToken { get; set; }
		public string RefreshToken { get; set; }
		public string Expiration { get; set; }
		public string Password { get; set; }
	}

	public class ActivateKeyModel 
	{
		public string Key { get; set;}
	}
	public class GetUserProfileImageModel 
	{
		public string Username { get; set; }
	}
	public class LoadProfileImageModel {
		public IFormFile File { get; set; }
	}
}