namespace SasavnServer.Controllers.Users
{
    public class AllMainInfoAboutSHBHLOL
    {
        public int UsersOnline { get; set; }
        public int TotalUsers { get; set; }
        public int TotalNonFreeUsers { get; set; }
        public int TotalTime { get; set; }
        public int TotalLogins { get; set; }
        public string MenuVersion { get; set; }
        public string LauncherVersion { get; set; }
    }

    public class PartialUser
    {
        public long Id { get; set; }
        public string Login { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string AuthToken { get; set; }
        public string Emailaccept { get; set; }
        public string money { get; set; }
        public int? Role { get; set; }
        public int? Emailaccepted { get; set; }
        public string DSUserID { get; set; }
        public string DSUserName { get; set; }
        public int? VKId { get; set; }
        public string VKTempKey { get; set; }
        public string Avatar { get; set; }
        public PartialUser() { }
    }



}
