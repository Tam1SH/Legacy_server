using System.ComponentModel.DataAnnotations;

namespace SasavnServer.Controllers.Assets
{
	public class CreateAssetModel {

		[RegularExpression(@"^[a-zA-Z0-9]+$")]
		public string Name { get; set; }
		public string Description { get; set; }
		public IFormFile Zip { get; set; }
	}
	
	public class EditAssetModel {
		public int Id { get; set; }

		[RegularExpression(@"^[a-zA-Z0-9]+$")]
		public string Name { get; set; }
		//TODO: ассоциировать с юзером, а не просто автор блять?
		public string Author { get; set; }
		public string Description { get; set; }
		public string SavedContents { get; set; }
		public string SavedPictures { get; set; }

	}

	public class GetAssetsModel {

		[Range(0, int.MaxValue)]
		public int Count { get; set; }		
		[Range(0, int.MaxValue)]
		public int Offset { get; set; }
	}

}