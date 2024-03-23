using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using SasavnServer.Model;
using Shared;


namespace SasavnServer.Repositories
{

	class TokensAndUser
    {
        public User User { get; set; }
        public Tokens Tokens { get; set; }
    }
    class TokensAndReseller
    {
        public Reseller Reseller { get; set; }
        public Tokens Tokens { get; set; }
    }

	//TODO: use redis
	//PUBLIC: на новом проекте редис (честно)
    public class UserRepository : IUserRepository
    {
        private readonly DataBaseContext _dbContext;
        private IMemoryCache cache;


		public void Migrate() {
			_dbContext.Migrate();
		}

        public UserRepository(DataBaseContext dbContext, IMemoryCache cache)
        {

            _dbContext = dbContext;
            this.cache = cache;
        }


        

        public async IAsyncEnumerable<User> AllAsync()
        {
            await foreach (var user in _dbContext.Users.AsAsyncEnumerable())
            {
                yield return user;
            }

        }
        public IQueryable<User> All()
        {
            return _dbContext.Users.AsQueryable();
        }

        public IQueryable<SettingsData> Settings()
        {
            return _dbContext.Settings.AsQueryable();
        }

        public void Update(SettingsData data, bool inTransaction)
        {
            _dbContext.Settings.Update(data);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public IQueryable<KeyCodes> GetAllKeys()
        {
            return _dbContext.Keycodes.AsQueryable();
        }

        public IQueryable<Reseller> Resellers()
        {
            return _dbContext.Resellers.AsQueryable();
        }

        public IQueryable<Tokens> Tokens()
        {
            return _dbContext.Tokens.AsQueryable();
        }

        public IQueryable<Stats> Stats()
        {
            return _dbContext.Stats.AsQueryable();
        }

        public IQueryable<Subscription> Subscriptions()
        {
            return _dbContext.Subscriptions.AsQueryable();
        }

        public void Add(Stats stats, bool inTransaction = false)
        {
            _dbContext.Stats.Add(stats);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }


        public void Add(Tokens tokens, bool inTransaction)
        {
            _dbContext.Tokens.Add(tokens);
            if (!inTransaction)
                _dbContext.SaveChanges();

        }

        public void Update(Tokens tokens, bool inTransaction)
        {
            _dbContext.Tokens.Update(tokens);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }


        public void Add(User user, bool inTransaction)
        {
            _dbContext.Users.Add(user);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public void Update(Reseller reseller, bool inTransaction)
        {
            _dbContext.Resellers.Update(reseller);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public void Update(KeyCodes keycode, bool inTransaction)
        {
            _dbContext.Keycodes.Update(keycode);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public void Update(User user, bool inTransaction)
        {
            _dbContext.Users.Update(user);

            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public IDbContextTransaction BeginTransaction()
        {
            return _dbContext.Database.BeginTransaction();
        }

        public void Save()
        {
            _dbContext.SaveChanges();
        }

        public User FindById(long id)
        {
            return _dbContext.Users.Find(id)!;
        }

        public void Add(KeyCodes keycode, bool inTransaction = false)
        {
            _dbContext.Keycodes.Add(keycode);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public IQueryable<Updates> GetUpdates()
        {
            return _dbContext.Updates.AsQueryable();
        }

		public IQueryable<GameUser> GameUsers()
		{
            return _dbContext.GameUser.AsQueryable();
		}


        public void Add(Updates updates, bool inTransaction = false)
        {
            _dbContext.Updates.Add(updates);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public void Update(Updates updates, bool inTransaction = false)
        {
            _dbContext.Updates.Update(updates);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }


        public void DeleteChangelog(long id, bool inTransaction = false)
        {
            _dbContext.Updates.Remove(_dbContext.Updates.Find(id));
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public void Remove(Tokens tokens, bool inTransaction = false)
        {
            _dbContext.Tokens.Remove(tokens);

            if (cache.TryGetValue(tokens.RefreshToken, out _))
            {
                cache.Remove(tokens.RefreshToken);
            }

            if (cache.TryGetValue(tokens.UserId, out Tokens[] tokens1))
            {
                cache.Remove(tokens.UserId);
            }

            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public void Add(Subscription subscription, bool inTransaction = false)
        {
            _dbContext.Subscriptions.Add(subscription);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public async Task<Tokens?> GetTokensByRefreshToken(string? refreshToken)
        {

            var token = await _dbContext.Tokens.SingleAsync(t => t.RefreshToken == refreshToken);
            if (cache.TryGetValue(token.RefreshToken, out _))
            {
                cache.Set(token.RefreshToken, token,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
            }
            return token;


        }

        public async Task<User> GetUserByLoginOrEmail(string loginOrEmail)
        {
            throw new NotImplementedException();

            var user = await _dbContext.Users.SingleAsync(u => u.Login == loginOrEmail || u.Email == loginOrEmail);
            if (cache.TryGetValue(new Tuple<string, string>(loginOrEmail, loginOrEmail), out _))
            {
                //cache.Set(token.RefreshToken, token,
                //    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
            }
            return user;
        }

        public async Task<User?> FindUserById(long id)
        {

            if (cache.TryGetValue(id, out TokensAndUser tokensAndUser))
            {
                if (tokensAndUser != null && tokensAndUser.User is not null)
                    return tokensAndUser.User;
            }

            tokensAndUser ??= new TokensAndUser();

            tokensAndUser.User = await _dbContext.Users.FindAsync(id);

            if (tokensAndUser.User is not null)
                cache.Set(id, tokensAndUser,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));

            return tokensAndUser.User;
        }

        public async Task<Reseller?> FindResellerByRefreshToken(string token)
        {

            if (cache.TryGetValue(token, out TokensAndReseller tokensAndUser))
            {
                if (tokensAndUser != null && tokensAndUser.Reseller != null)
                    return tokensAndUser.Reseller;
            }

            tokensAndUser ??= new TokensAndReseller();


            var token_ = await _dbContext.Tokens.FindAsync(token);

            if (token_ != null)
                tokensAndUser.Reseller = await _dbContext.Resellers.FindAsync(token_.UserId - Reseller.MagicOffset);

            if (tokensAndUser.Reseller is not null)
                cache.Set(token, tokensAndUser,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));

            return tokensAndUser.Reseller;

        }

        public async Task<User?> FindUserByRefreshToken(string? token)
        {
            if (token is null)
                return null;

            if (cache.TryGetValue(token, out TokensAndUser tokensAndUser))
            {
                if (tokensAndUser != null && tokensAndUser.User != null)
                    return tokensAndUser.User;
            }

            tokensAndUser ??= new TokensAndUser();
            

            var token_ = await _dbContext.Tokens.FindAsync(token);

            if (token_ != null)
                tokensAndUser.User = await _dbContext.Users.FindAsync(token_.UserId);

            if (tokensAndUser.User is not null)
                cache.Set(token, tokensAndUser,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));

            return tokensAndUser.User;

        }

        public async Task<User?> FindUserByLogin(string login)
        {
            if (cache.TryGetValue(login, out TokensAndUser tokensAndUser))
            {
                if (tokensAndUser != null && tokensAndUser.User != null)
                    return tokensAndUser.User;
            }

            tokensAndUser ??= new TokensAndUser();


            tokensAndUser.User = await _dbContext.Users.SingleAsync(u => u.Login == login);

            if (tokensAndUser.User is not null)
                cache.Set(login, tokensAndUser,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));


            return tokensAndUser.User;
        }

        public async Task<Tokens?> FindTokensByRefreshToken(string? refreshToken)
        {

            var tokensAndUser = new TokensAndUser();

            tokensAndUser.Tokens = await _dbContext.Tokens.FindAsync(refreshToken);

            if (tokensAndUser.Tokens is not null)
                cache.Set(refreshToken, tokensAndUser,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));



            return tokensAndUser.Tokens;

        }

        public UserRepositoryContextTransaction BeginTransaction2()
        {
            return new UserRepositoryContextTransaction(this);
        }

        public async Task SaveAsync()
        {
            await _dbContext.SaveChangesAsync();
        }

        public async Task<T> BeginTransaction<T>(Func<UserRepositoryContextTransaction, Task<T>> action)
        {
            using var trans = new UserRepositoryContextTransaction(this);
            try
            {
                var result = await action(trans);
                await trans.CommitAsync();
                return result;
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }
                
        }

        public T BeginTransaction<T>(Func<UserRepositoryContextTransaction, T> action)
        {
            using var trans = new UserRepositoryContextTransaction(this);
            try
            {
                var result = action(trans);
                trans.Commit();
                return result;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public void BeginTransaction(Action<UserRepositoryContextTransaction> action)
        {
            using var trans = new UserRepositoryContextTransaction(this);
            try
            {
                action(trans);
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }


        public async Task BeginTransaction(Func<UserRepositoryContextTransaction, Task> action)
        {
            using var trans = new UserRepositoryContextTransaction(this);
            try
            {
                await action(trans);
                await trans.CommitAsync();
            }
            catch
            {
                await trans.RollbackAsync();
                throw;
            }

        }

        public async Task<Tokens[]?> FindTokensByUserId(long? userId)
        {
            if (cache.TryGetValue(userId, out Tokens[]? tokens))
                return tokens;

            tokens = await _dbContext.Tokens.Where(t => t.UserId == userId).ToArrayAsync();

            if (tokens is not null)
                cache.Set(userId, tokens,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));

            return tokens;


        }

        public async Task<Socials?> FindSocialByServiceId(string id)
        {
            if (cache.TryGetValue(id, out Socials? socials))
            {
                return socials;
            }

            socials = await _dbContext.Socials.
                SingleOrDefaultAsync(s => 
                    s.VKID == id || s.GoogleId == id || s.DiscordId == id || s.SteamId == id
                );

            if (socials is not null)
                cache.Set(id, socials,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));

            return socials;
        }

        public void Add(Socials socials, bool inTransaction = false)
        {
            _dbContext.Socials.Add(socials);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public async Task<Socials?> FindSocialByUserId(long id)
        {
            if (cache.TryGetValue(id, out Socials socials))
            {
                return socials;
            }

            socials = await _dbContext.Socials.FindAsync(id);

            if (socials is not null)
                cache.Set(id, socials,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));

            return socials;
        }

        public void Remove(Socials socials, bool inTransaction = false)
        {
            _dbContext.Socials.Remove(socials);

            if (cache.TryGetValue(socials.UserId, out _))
            {
                cache.Remove(socials.UserId);
            }

            if (!inTransaction)
                _dbContext.SaveChanges();

        }

        public void Update(Socials socials, bool inTransaction = false)
        {
            _dbContext.Socials.Update(socials);

            if (cache.TryGetValue(socials.UserId, out _))
            {
                cache.Remove(socials.UserId);
            }

            if (!inTransaction)
                _dbContext.SaveChanges();

        }

        public IQueryable<Socials> Socials()
        {
            return _dbContext.Socials;
        }

        public async Task<SocialUserCommon?> FindCommonSocialByServiceId(string serviceId)
        {
            if (cache.TryGetValue(serviceId, out SocialUserCommon? socialsCommon))
                return socialsCommon;


            socialsCommon = await _dbContext.SocialUserCommon.FindAsync(serviceId);

            if (socialsCommon is not null)
                cache.Set(serviceId, socialsCommon,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));

            return socialsCommon;

        }

        public async Task<SocialUserCommon[]> FindAllCommonSocialByUserId(long userId)
        {
            if (cache.TryGetValue(userId, out SocialUserCommon[] socialsCommon))
                return socialsCommon;

            socialsCommon = await _dbContext.SocialUserCommon.Where(s => s.UserId == userId).ToArrayAsync();

            if (socialsCommon is not null)
                cache.Set(userId, socialsCommon,
                    new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));

            return socialsCommon ?? Array.Empty<SocialUserCommon>();
        }

        public void Add(SocialUserCommon socialCommon, bool inTransaction = false)
        {
            _dbContext.SocialUserCommon.Add(socialCommon);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public void Update(SocialUserCommon socialCommon, bool inTransaction = false)
        {
            _dbContext.SocialUserCommon.Update(socialCommon);
            if (!inTransaction)
                _dbContext.SaveChanges();
        }

        public void Remove(SocialUserCommon socialCommon, bool inTransaction = false)
        {
            _dbContext.SocialUserCommon.Remove(socialCommon);

            if (cache.TryGetValue(socialCommon.ServiceId, out _))
            {
                cache.Remove(socialCommon.ServiceId);
            }

            if (!inTransaction)
                _dbContext.SaveChanges();

        }

        public async Task RemoveCommonSocialByServiceId(string serviceId)
        {
            SocialUserCommon? socialsCommon = await _dbContext.SocialUserCommon.FindAsync(serviceId);

            if (cache.TryGetValue(serviceId, out _))
            {
                cache.Remove(serviceId);
            }

            _dbContext.SocialUserCommon.Remove(socialsCommon);

        }

		public GameUser Add(GameUser gameUser, bool inTransaction = false)
		{
            var ent = _dbContext.GameUser.Add(gameUser);
            if (!inTransaction)
                _dbContext.SaveChanges();

            return ent.Entity;
		}

		public void Update(Subscription subscription, bool inTransaction = false)
		{
            _dbContext.Subscriptions.Update(subscription);
            if (!inTransaction)
                _dbContext.SaveChanges();
		}

		public void Update(GameUser gameUser, bool inTransaction = false)
		{
            _dbContext.GameUser.Update(gameUser);
            if (!inTransaction)
                _dbContext.SaveChanges();
		}

		//public IQueryable<NewUser> NewUsers()
		//{
		//	return _dbContext.NewUser;
		//}
	}
}
