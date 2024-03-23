using Microsoft.EntityFrameworkCore.Storage;

namespace SasavnServer.Repositories
{
    public class AssetsStore : IAssetsStore
    {
        private readonly DataBaseContext _dbContext;
       // private IMemoryCache cache;
        public AssetsStore(DataBaseContext dbContext//, IMemoryCache cache
            )
        {
            _dbContext = dbContext;
         //   this.cache = cache;
        }
        public async Task Save()
        {
            await _dbContext.SaveChangesAsync();
        }
        public void Add(AssetInfo assetInfo, bool inTransaction = false)
        {
            _dbContext.AssetInfo.Add(assetInfo);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public void Add(Content content, bool inTransaction = false)
        {
            _dbContext.Content.Add(content);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public void Add(PresentImages presentImages, bool inTransaction = false)
        {
            _dbContext.PresentImages.Add(presentImages);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public IQueryable<AssetInfo> All()
        {
            return _dbContext.AssetInfo;
        }

        public IDbContextTransaction BeginTransaction()
        {
            return _dbContext.Database.BeginTransaction();
        }

        public void Delete(long id, bool inTransaction = false)
        {
            var asset = FindByIdAsset(id);
            if (asset != null)
            {
                _dbContext.AssetInfo.Remove(asset);
                if (!inTransaction) 
                    _dbContext.SaveChanges();
            }
        }

        public void DeleteContent(long id, bool inTransaction = false)
        {
            var contents = FindByIdContent(id).ToArray();
            if (contents != null)
            {
                foreach(var content in contents)
                {
                    _dbContext.Content.Remove(content);
                }
                if (!inTransaction)
                    _dbContext.SaveChanges();
            }
        }

        public void DeleteImage(long id, bool inTransaction = false)
        {
            var images = FindByIdImages(id).ToArray();
            if (images != null)
            {
                foreach (var image in images)
                {
                    _dbContext.PresentImages.Remove(image);
                }
                if (!inTransaction)
                    _dbContext.SaveChanges();
            }
        }

        public AssetInfo? FindByIdAsset(long id)
        {
            return _dbContext.AssetInfo.Find(id);
        }

        public IEnumerable<Content> FindByIdContent(long id)
        {
            return _dbContext.Content.Where(c => c.ContentId == id);
        }

        public IEnumerable<PresentImages> FindByIdImages(long id)
        {
            return _dbContext.PresentImages.Where(c => c.ImageId == id);
        }

        public void Update(AssetInfo assetInfo, bool inTransaction = false)
        {
            _dbContext.AssetInfo.Update(assetInfo);
            if(!inTransaction)
                _dbContext.SaveChanges();
        }

        public void Update(Content content, bool inTransaction = false)
        {
            _dbContext.Content.Update(content);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public void Update(PresentImages presentImages, bool inTransaction = false)
        {
            _dbContext.PresentImages.Update(presentImages);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public void Delete(AssetInfo asset, bool inTransaction = false)
        {
            _dbContext.AssetInfo.Remove(asset);
            if(!inTransaction)
                _dbContext.SaveChanges();
        }

        public void DeleteContent(Content content, bool inTransaction = false)
        {
            _dbContext.Content.Remove(content);
            if(!inTransaction)
                _dbContext.SaveChanges();

        }

        public void DeleteImage(PresentImages images, bool inTransaction = false)
        {
            _dbContext.PresentImages.Remove(images);
            if(!inTransaction)
                _dbContext.SaveChanges();

        }
    }
}
