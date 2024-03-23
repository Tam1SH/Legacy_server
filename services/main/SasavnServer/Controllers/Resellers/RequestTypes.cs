namespace SasavnServer.Controllers.Resellers
{
	public enum KeyType {
		Default = 1, HWID = 4
	}
	public class GetKeysForResellerModel {
		public KeyType KeyType { get; set; }
	}

	public class GenKeyModel {
		public KeyType KeyType { get; set; }
		public int KeysAmount { get; set; }
	}
}