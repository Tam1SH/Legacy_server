using System.Text.Json.Serialization;

namespace SasavnServer.Controllers.Updater
{
	public class UpdateResult {

		public class InnerField {
			
		}
		public Version Version { get; set; }
    	[JsonPropertyName("pub_date")]
		public DateTime PubDate { get; set; }
		public string Url { get; set; }
		public string Signature { get; set; }
		public string Notes { get; set; }
	}
}