using System.ComponentModel.DataAnnotations;

namespace SasavnServer.Controllers.Authenticate
{
	public class LoginFromServiceModel
	{
		public string ClientName { get; set; }
		public string State { get; set; }
	}

	public class LoginModel
	{
		public string Login { get; set; }
		public string Password { get; set; }
		public LoginFor Type { get; set; }
		public string ClientName { get; set; }

	}

	public class CloseSessionModel 
	{
		public string RefreshToken { get; set; }
	}
	public class DefaultLoginModel {
		public bool Rebind { get; set; }
		public string? ReplyTo {get; set; }
	}

	public class ConfirmRegistrationModel
	{
		public string Uuid { get; set; }
	}
	public class ConfirmPasswordModel 
	{
		public string Password { get; set; }
	}
	public class ResetPasswordModel 
	{
		public string Uuid { get; set; }
		public string Password { get; set; }
	}

	public class RecoveryAccountByEmailModel 
	{
		[EmailAddress]
		public string Email { get; set; }
	}
	public class RewritePasswordFromRecoveryModel 
	{
		public string Uuid { get; set; }
		public string Password { get; set; }
	}

	public class AcceptFromEmailModel
	{
		[StringLength(30)]
		[MinLength(4)]
		[RegularExpression(@"^[a-zA-Z0-9]+$")]
		public string Login { get; set; }
		[EmailAddress]
		public string Email { get; set; }
		public string Password { get; set; }
		public string Secret { get; set; }
	}
}