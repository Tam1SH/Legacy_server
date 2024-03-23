using System.Text;

namespace Shared
{
	public class Settings
    {
        public static string SqlKey { get; set; }

        static Settings()
        {
            ShopId = Encoding.UTF8.GetString(Convert.FromBase64String("TGHFVABR"));
        }

        public static string YandexCheckoutKey { get; }
        public static string ShopId { get; set; }

    }
}
