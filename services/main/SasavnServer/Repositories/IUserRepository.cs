
using Microsoft.EntityFrameworkCore.Storage;
using SasavnServer.Model;
using Shared;

namespace SasavnServer.Repositories
{

	
    public interface IUserRepository
    {
        IQueryable<Reseller> Resellers();

        IQueryable<KeyCodes> GetAllKeys();

        IQueryable<Updates> GetUpdates();

        IQueryable<Tokens> Tokens();

        IQueryable<Stats> Stats();

        IQueryable<Socials> Socials();

        IQueryable<Subscription> Subscriptions();

		IQueryable<GameUser> GameUsers();

        IAsyncEnumerable<User> AllAsync();

        IQueryable<User> All();

        IQueryable<SettingsData> Settings();

        IDbContextTransaction BeginTransaction();

        T BeginTransaction<T>(Func<UserRepositoryContextTransaction, T> action);

        void BeginTransaction(Action<UserRepositoryContextTransaction> action);

        Task BeginTransaction(Func<UserRepositoryContextTransaction, Task> action);

        Task<T> BeginTransaction<T>(Func<UserRepositoryContextTransaction, Task<T>> action);

        Task<Tokens?> GetTokensByRefreshToken(string? refreshToken);

        Task<User> GetUserByLoginOrEmail(string loginOrEmail);

        void Save();

        Task SaveAsync();

        Task<User?> FindUserById(long id);

        Task<User?> FindUserByRefreshToken(string? refreshToken);

        Task<User?> FindUserByLogin(string login);

        Task<SocialUserCommon?> FindCommonSocialByServiceId(string serviceId);

        Task<SocialUserCommon[]> FindAllCommonSocialByUserId(long userId);

        Task<Reseller?> FindResellerByRefreshToken(string token);

        Task<Socials?> FindSocialByServiceId(string id);

        Task<Socials?> FindSocialByUserId(long id);

        Task<Tokens?> FindTokensByRefreshToken(string? refreshToken);

        Task<Tokens[]?> FindTokensByUserId(long? userId);

        User FindById(long id);

        void DeleteChangelog(long id, bool inTransaction = false);

        void Add(Stats stats, bool inTransaction = false);
        void Add(Socials socials, bool inTransaction = false);
        void Add(Subscription subscription, bool inTransaction = false);
        void Add(User user, bool inTransaction = false);
        void Add(Tokens tokens, bool inTransaction = false);
        void Add(Updates updates, bool inTransaction = false);
        void Add(KeyCodes keycode, bool inTransaction = false);
        void Add(SocialUserCommon socialCommon, bool inTransaction = false);
        GameUser Add(GameUser gameUser, bool inTransaction = false);

        void Update(SocialUserCommon socialCommon, bool inTransaction = false);
        void Update(Socials socials, bool inTransaction = false);
        void Update(Tokens tokens, bool inTransaction = false);
        void Update(Updates updates, bool inTransaction = false);
        void Update(SettingsData data, bool inTransaction = false);
        void Update(User user, bool inTransaction = false);
        void Update(Reseller reseller, bool inTransaction = false);
        void Update(KeyCodes keycode, bool inTransaction = false);
        void Update(Subscription subscription, bool inTransaction = false);
        void Update(GameUser subscription, bool inTransaction = false);

        Task RemoveCommonSocialByServiceId(string serviceId);

        void Remove(SocialUserCommon socialCommon, bool inTransaction = false);
        void Remove(Tokens tokens, bool inTransaction = false);
        void Remove(Socials socials, bool inTransaction = false);
    }
}
