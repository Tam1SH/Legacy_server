using MailKit.Net.Smtp;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using System.Text;

namespace SiteAPI
{
	public class AuthOptions
    {
        public const string ISSUER = "Sasavn";
        public const string AUDIENCE = "Sasavn";
        const string KEY = "AE[ORKHG){_(%kb$5$#+%b_%$#O^%)$#_^i0=9GAK[EPR,OH[]1]}]";
        public const int LIFETIME = 60;
        public const int LIFETIME_REFRESH = 180;

        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }

    }

    public class EmailService
    {
        public string DefaultBody(string innerHTML)
        {

            var style = "width:100%;height:100%;";
            var headerStyle = "width:100%;";
            var tableStyle = "width=\"100%\" border=\"0\" cellspasing=\"0\" cellpadding=\"0\"";
			//PUBLIC: template generators better.
            return $@"
<html>
<head>
<style>
@font-face 
{"{"}
    font-family : {"SUS"};
    src : url(""https://sasavn.ru/static/media/pizdat_font.e00656e72f41a7cfe117.otf"");
{"}"}
</style>
</head>
<body>
<tbody>
<table {tableStyle} style={"border-radius:10px;background:#191919;width:600px;"}>
    <tbody style={style}>
        <tr>
            <div style={headerStyle}>
                <center>
                    <label style={"margin-left:10px;font-size:x-large;color:white;"}>SASAVN</label>
                </center>
            </div>

            <div style={"padding:30px;height:40px;"}>
                <center>
                    {innerHTML}
                </center>
            </div>
        </tr>
    </tbody>
</table>
</tbody>
</body>
</html>

";

        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress("___SEC_AMOGUS___", "___SEC_AMOGUS___"));
                emailMessage.To.Add(new MailboxAddress("___SEC_AMOGUS___", email));
                emailMessage.Subject = subject;
                emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = message,
                };

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync("___SEC_AMOGUS___", 1337, true);
                    await client.AuthenticateAsync("___SEC_AMOGUS___", "___SEC_AMOGUS___");
                    await client.SendAsync(emailMessage);
                    await client.DisconnectAsync(true);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"email : {ex}");
            }
        }


    }
}
