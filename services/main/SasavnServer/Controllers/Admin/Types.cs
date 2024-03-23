
using SasavnServer.Repositories;

namespace SasavnServer.Controllers.Admin
{

	public class MetaData
	{
		public long? Length { get; set; }
		public string CreationDate { get; set; }
	}
	public class Entry
	{
		public string Name { get; set; }
		public bool IsFile { get; set; }
		public MetaData MetaData { get; set; }
		public Entry[]? Children { get; set; }
	}

    public class TotalUser
    {
        public long Id { get; set; }
        public string Login { get; set; }
        public string Email { get; set; }
        public string Salt { get; set; }
        public long Money { get; set; }
        public string[] Roles { get; set; }
        public string? HWID { get; set; }
        public string? AuthToken { get; set; }
        public DateTime? HwidResetDate { get; set; }
		public Subscription[]? Subscriptions { get; set;}
    }


}

