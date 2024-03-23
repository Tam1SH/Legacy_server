using Microsoft.EntityFrameworkCore;

namespace SasavnServer.Repositories
{



	public interface ITransactionRepository
    {
        public Task<Transaction> GetByTSHashAsync(string TSHash);

        public void Update(Transaction transaction);

        IQueryable<Transaction> GetAll();

        public Transaction Add(Transaction transaction);

    }

    public class TransactionRepository : ITransactionRepository
    {

        private readonly DataBaseContext _dbContext;

        public TransactionRepository(DataBaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Transaction Add(Transaction transaction)
        {
            var trans = _dbContext.Transactions.Add(transaction);
            _dbContext.SaveChanges();

            return trans.Entity;
        }

        public IQueryable<Transaction> GetAll()
        {
            return _dbContext.Transactions.AsQueryable();
        }

        public async Task<Transaction> GetByTSHashAsync(string TSHash)
        {
            return await _dbContext.Transactions.SingleAsync(t => t.TSHash == TSHash);
        }

        public void Update(Transaction transaction)
        {
            _dbContext.Transactions.Update(transaction);
            _dbContext.SaveChanges();
        }
    }
}
