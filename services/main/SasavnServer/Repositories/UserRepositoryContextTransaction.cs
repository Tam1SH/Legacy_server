
using Microsoft.EntityFrameworkCore.Storage;
using SasavnServer.Model;
using Shared;

namespace SasavnServer.Repositories
{
    public class UserRepositoryContextTransaction : IUserRepository, IDbContextTransaction, IDisposable
    {

        private IUserRepository userRepository;

        private IDbContextTransaction dbContextTransaction;

        public Guid TransactionId => dbContextTransaction.TransactionId;

        public UserRepositoryContextTransaction(IUserRepository userRepository)
        {
            this.userRepository = userRepository;
            dbContextTransaction = userRepository.BeginTransaction();
        }

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public Task<Tokens?> GetTokensByRefreshToken(string? refreshToken)
        {
            return userRepository.GetTokensByRefreshToken(refreshToken);
        }

        public IQueryable<Reseller> Resellers()
        {
            return userRepository.Resellers();
        }

        public IQueryable<KeyCodes> GetAllKeys()
        {
            return userRepository.GetAllKeys();
        }

        public IQueryable<Updates> GetUpdates()
        {
            return userRepository.GetUpdates();
        }

        public IQueryable<Tokens> Tokens()
        {
            return userRepository.Tokens();
        }

        public IQueryable<Stats> Stats()
        {
            return userRepository.Stats();
        }

        public IQueryable<Subscription> Subscriptions()
        {
            return userRepository.Subscriptions();
        }

        public IAsyncEnumerable<User> AllAsync()
        {
            return userRepository.AllAsync();
        }

        public IQueryable<User> All()
        {
            return userRepository.All();
        }

        public IQueryable<SettingsData> Settings()
        {
            return userRepository.Settings();
        }

        public IDbContextTransaction BeginTransaction()
        {
            return userRepository.BeginTransaction();
        }

        public Task<User> GetUserByLoginOrEmail(string loginOrEmail)
        {
            return userRepository.GetUserByLoginOrEmail(loginOrEmail);
        }

        public void Save()
        {
            userRepository.Save();
        }



        public User FindById(long id)
        {
            return userRepository.FindById(id);
        }

        public void DeleteChangelog(long id, bool inTransaction = false)
        {
            userRepository.DeleteChangelog(id, true);
        }

        public void Add(Subscription subscription, bool inTransaction = false)
        {
            userRepository.Add(subscription, true);
        }

        public void Add(User user, bool inTransaction = false)
        {
            userRepository.Add(user, true);
        }

        public void Add(Tokens tokens, bool inTransaction = false)
        {
            userRepository.Add(tokens, true);
        }

        public void Add(Updates updates, bool inTransaction = false)
        {
            userRepository.Add(updates, true);
        }

        public void Add(KeyCodes keycode, bool inTransaction = false)
        {
            userRepository.Add(keycode, true);
        }

        public void Add(Stats stats, bool inTransaction = false)
        {
            userRepository.Add(stats, true);
        }

        public void Update(Tokens tokens, bool inTransaction = false)
        {
            userRepository.Update(tokens,   true);
        }

        public void Update(Updates updates, bool inTransaction = false)
        {
            userRepository.Update(updates, true);
        }

        public void Update(SettingsData data, bool inTransaction = false)
        {
            userRepository.Update(data, true);
        }

        public void Update(User user, bool inTransaction = false)
        {
            userRepository.Update(user, true);
        }

        public void Update(Reseller reseller, bool inTransaction = false)
        {
            userRepository.Update(reseller, true);
        }

        public void Update(KeyCodes keycode, bool inTransaction = false)
        {
            userRepository.Update(keycode, true);
        }

        public void Remove(Tokens? tokens, bool inTransaction = false)
        {
            userRepository.Remove(tokens!, true);
        }

        public Task<User?> FindUserById(long id)
        {
            return userRepository.FindUserById(id);
        }

        public Task<User?> FindUserByRefreshToken(string? refreshToken)
        {
            return userRepository.FindUserByRefreshToken(refreshToken);
        }

        public Task<User?> FindUserByLogin(string login)
        {
            return userRepository.FindUserByLogin(login);
        }

        public Task<Tokens?> FindTokensByRefreshToken(string? refreshToken)
        {
            return userRepository.FindTokensByRefreshToken(refreshToken);
        }

        public void Commit()
        {
            userRepository.Save();

            dbContextTransaction.Commit();
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await userRepository.SaveAsync();

            await dbContextTransaction.CommitAsync(cancellationToken);

        }

        public void Rollback()
        {
            dbContextTransaction.Rollback();
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return dbContextTransaction.RollbackAsync(cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return dbContextTransaction.DisposeAsync();
        }

        public Task SaveAsync()
        {
            return userRepository.SaveAsync();
        }

        public Task<T> BeginTransaction<T>(Func<UserRepositoryContextTransaction, Task<T>> action)
        {
            return userRepository.BeginTransaction(action);
        }

        public Task<Tokens[]?> FindTokensByUserId(long? userId)
        {
            return userRepository.FindTokensByUserId(userId);
        }

        public Task<Socials?> FindSocialByServiceId(string id)
        {
            return userRepository.FindSocialByServiceId(id);
        }

        public void Add(Socials socials, bool inTransaction = false)
        {
            userRepository.Add(socials, true);
        }

        public Task<Socials?> FindSocialByUserId(long id)
        {
            return userRepository.FindSocialByUserId(id);
        }

        public void Remove(Socials socials, bool inTransaction = false)
        {
            userRepository.Remove(socials, true);
        }

        public void Update(Socials socials, bool inTransaction = false)
        {
            userRepository.Update(socials, true);
        }

        public IQueryable<Socials> Socials()
        {
            return userRepository.Socials();
        }

        public Task<SocialUserCommon?> FindCommonSocialByServiceId(string serviceId)
        {
            return userRepository.FindCommonSocialByServiceId(serviceId);
        }

        public Task<SocialUserCommon[]> FindAllCommonSocialByUserId(long userId)
        {
            return userRepository.FindAllCommonSocialByUserId(userId);
        }

        public void Add(SocialUserCommon socialCommon, bool inTransaction = false)
        {
            userRepository.Add(socialCommon, true);
        }

        public void Update(SocialUserCommon socialCommon, bool inTransaction = false)
        {
            userRepository.Update(socialCommon, true);
        }

        public void Remove(SocialUserCommon socialCommon, bool inTransaction = false)
        {
            userRepository.Remove(socialCommon, true);
        }

        public Task BeginTransaction(Func<UserRepositoryContextTransaction, Task> action)
        {
            return userRepository.BeginTransaction(action);
        }

        public T BeginTransaction<T>(Func<UserRepositoryContextTransaction, T> action)
        {
            return userRepository.BeginTransaction(action);
        }

        public void BeginTransaction(Action<UserRepositoryContextTransaction> action)
        {
            userRepository.BeginTransaction(action);
        }

        public Task RemoveCommonSocialByServiceId(string serviceId)
        {
            return userRepository.RemoveCommonSocialByServiceId(serviceId);
        }

		public IQueryable<GameUser> GameUsers()
		{
			return userRepository.GameUsers();
		}

		public GameUser Add(GameUser gameUser, bool inTransaction = false)
		{
			return userRepository.Add(gameUser, inTransaction);
		}

		public void Update(Subscription subscription, bool inTransaction = false)
		{
			userRepository.Update(subscription, inTransaction);
		}

		public void Update(GameUser subscription, bool inTransaction = false)
		{
			userRepository.Update(subscription, inTransaction);
		}

        public Task<Reseller?> FindResellerByRefreshToken(string token)
        {
            return userRepository.FindResellerByRefreshToken(token);
        }

        //public IQueryable<NewUser> NewUsers()
        //{
        //	return userRepository.NewUsers();
        //}
    }
}
