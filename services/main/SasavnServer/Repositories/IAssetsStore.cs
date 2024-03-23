using Microsoft.EntityFrameworkCore.Storage;

namespace SasavnServer.Repositories
{

    public class AssetInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; } 
        public int? PresentImages { get; set; } 
        public int? Content { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
        public DateTime? Date { get; set; }
        public int CountInstall { get; set; }
        public int ViewCount { get; set; }
    }

    public class PresentImages 
    {
        public int Id { get; set; }
        public int ImageId { get; set; }
        public string ImagePath { get; set; }

    }

    public class Content
    {
        public int Id { get; set; }
        public int ContentId { get; set; }
        public string ContentPath { get; set; }
    }

    public interface IAssetsStore
    {
        IQueryable<AssetInfo> All();
        IDbContextTransaction BeginTransaction();

        AssetInfo? FindByIdAsset(long id);
        IEnumerable<Content> FindByIdContent(long id);
        IEnumerable<PresentImages> FindByIdImages(long id);

        void Add(AssetInfo assetInfo, bool inTransaction = false);
        void Add(Content content, bool inTransaction = false);
        void Add(PresentImages presentImages, bool inTransaction = false);

        void Update(AssetInfo assetInfo, bool inTransaction = false);
        void Update(Content content, bool inTransaction = false);
        void Update(PresentImages presentImages, bool inTransaction = false);

        void Delete(long id, bool inTransaction = false);
        void DeleteContent(long id, bool inTransaction = false);
        void DeleteImage(long id, bool inTransaction = false);

        void Delete(AssetInfo asset, bool inTransaction = false);
        void DeleteContent(Content content, bool inTransaction = false);
        void DeleteImage(PresentImages images, bool inTransaction = false);

        Task Save();

    }
}
