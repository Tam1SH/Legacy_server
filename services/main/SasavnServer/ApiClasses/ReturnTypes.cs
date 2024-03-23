namespace SasavnServer.ApiClasses
{
    public class DefaultReturn
    {
        public DefaultReturn() { }
        public DefaultReturn(long _result, string _message)
        {
            result = _result;
            message = _message;
        }
        public long result { get; set; }
        public string message { get; set; }
    }

    public class ChangelogCount
    {
        public int count { get; set; }
        public string[] keys { get; set; }
    }

    public class AuthData
    {
        public string login { get; set; }
        public string password { get; set; }
    }
    public class DllVersionAsk
    {
        public string game { get; set; }
    }
    public class DllAuthData : DllVersionAsk
    {
        public string login { get; set; }
        public string password { get; set; }
        public string HWID { get; set; }
    }
    public class CapthaResult
    {
        public bool success { get; set; }
        public string challenge_ts { get; set; }
        public string hostname { get; set; }

        public CapthaResult(bool success, string challenge_ts, string hostname)
        {
            this.success = success;
            this.challenge_ts = challenge_ts;
            this.hostname = hostname;

        }
    }

    public class RegistrationUserResult
    {
        public string login { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public string secret { get; set; }
    }

    public class ServerTickReturn : DefaultReturn
    {
        public int OnlineCount { get; set; }
    }

    public class ErrorCode
    {
        public ErrorCode(int code, string message)
        {
            Code = code;
            Message = message;
        }

        public int Code { get; set; }
        public string Message { get; set; }
    }
   


    public class AssetResult
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string Type { get; set; }
        public string[] presentImages { get; set; }
        public string Date { get; set; }
        public string Image { get; set; }
        public int CountInstall { get; set; }
        public class DescriptionIncludes
        {
            public string Description { get; set; }
            public string MediaContent { get; set; }

        }

    }
    public class CreateAssetResult : DefaultReturn
    {
        public bool isCreated;
    }

}

