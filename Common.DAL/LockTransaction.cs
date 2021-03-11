using Common.Lock;
using CommonFunction;
using System.Threading.Tasks;

namespace Common.DAL
{
    public static class ITransactionExtend
    {
        public static void Lock<T>(this ITransaction transaction, params string[] parameters)
        {
            ConventToILockTransaction(transaction).Lock<T>(parameters);
        }

        public static Task LockAsync<T>(this ITransaction transaction, params string[] parameters)
        {
            return ConventToILockTransaction(transaction).LockAsync<T>(parameters);
        }

        public static ILockTransaction ConventToILockTransaction(ITransaction transaction)
        {
            if (transaction.GetType().GetInterface(typeof(ILockTransaction).FullName) == null)
                transaction = new LockTransaction(transaction);

            return (ILockTransaction)transaction;
        }

        public static ITransaction ConvertToITransaction(this ITransaction transaction)
        {
            if (transaction != null && transaction is LockTransaction lockTransaction)
                return lockTransaction.Transaction;
            else
                return transaction;
        }
    }

    public interface ILockTransaction : ITransaction
    {
        void Lock<T>(params string[] parameters);

        Task LockAsync<T>(params string[] parameters);
    }

    public class LockTransaction : ILockTransaction
    {
        private const int LOCK_TIME_OUT = 5000;

        public ITransaction Transaction { get; }
        private readonly static ILock m_lock;
        private readonly string m_identity;

        static LockTransaction()
        {
            m_lock = LockFactory.GetRedisLock();
        }

        public LockTransaction(ITransaction transaction)
        {
            Transaction = transaction;
            m_identity = IDGenerator.NextID().ToString();
        }

        public object Context => Transaction.Context;

        public void Dispose()
        {
            Transaction.Dispose();
        }

        public void Lock<T>(params string[] parameters)
        {
            if (!m_lock.AcquireMutex(LockKeyGenerator.UniqueLockKeyGenerator(typeof(T), parameters), m_identity, 0, LOCK_TIME_OUT))
                throw new ResourceException("唯一键上锁失败。");
        }

        public async Task LockAsync<T>(params string[] parameters)
        {
            if (!await m_lock.AcquireMutexAsync(LockKeyGenerator.UniqueLockKeyGenerator(typeof(T), parameters), m_identity, 0, LOCK_TIME_OUT))
                throw new ResourceException("唯一键上锁失败。");
        }

        private void Release()
        {
            m_lock.Release(m_identity);
        }

        private async Task ReleaseAsync()
        {
            await m_lock.ReleaseAsync(m_identity);
        }

        public void Rollback()
        {
            Transaction.Rollback();
            Release();
        }

        public async Task RollbackAsync()
        {
            await Transaction.RollbackAsync();
            await ReleaseAsync();
        }

        public void Submit()
        {
            Transaction.Submit();
            Release();
        }

        public async Task SubmitAsync()
        {
            await Transaction.SubmitAsync();
            await ReleaseAsync();
        }
    }
}