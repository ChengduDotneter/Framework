using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DAL;

namespace Common.ServiceCommon
{
    public static class EditQueryExtend
    {
        public static void DeleteQueryPageSize<T>(this IEditQuery<T> editQuery, IEnumerable<long> ids, int pageSize = 50, ITransaction transaction = null)
            where T : class, IEntity, new()
        {
            int currentDeletePage = 0;
            while (ids.Count() > pageSize * currentDeletePage)
            {
                editQuery.Delete(transaction, ids.Skip(pageSize * currentDeletePage).Take(pageSize).ToArray());
                currentDeletePage++;
            }
        }

        public static async Task DeleteQueryPageSizeAsync<T>(this IEditQuery<T> editQuery, IEnumerable<long> ids, int pageSize = 50, ITransaction transaction = null)
                 where T : class, IEntity, new()
        {
            int currentDeletePage = 0;
            while (ids.Count() > pageSize * currentDeletePage)
            {
                await editQuery.DeleteAsync(transaction, ids.Skip(pageSize * currentDeletePage).Take(pageSize).ToArray());
                currentDeletePage++;
            }
        }

        public static void InsertQueryPageSize<T>(this IEditQuery<T> editQuery, IEnumerable<T> models, int pageSize = 50, ITransaction transaction = null)
            where T : class, IEntity, new()
        {
            int currentInsertPage = 0;
            while (models.Count() > pageSize * currentInsertPage)
            {
                editQuery.Insert(transaction, models.Skip(pageSize * currentInsertPage).Take(pageSize).ToArray());
                currentInsertPage++;
            }
        }

        public static async Task InsertQueryPageSizeAsync<T>(this IEditQuery<T> editQuery, IEnumerable<T> models, int pageSize = 50, ITransaction transaction = null)
             where T : class, IEntity, new()
        {
            int currentDeletePage = 0;
            while (models.Count() > pageSize * currentDeletePage)
            {
                await editQuery.InsertAsync(transaction, models.Skip(pageSize * currentDeletePage).Take(pageSize).ToArray());
                currentDeletePage++;
            }
        }

        public static void MergeQueryPageSize<T>(this IEditQuery<T> editQuery, IEnumerable<T> models, int pageSize = 50, ITransaction transaction = null)
           where T : class, IEntity, new()
        {
            int currentDeletePage = 0;
            while (models.Count() > pageSize * currentDeletePage)
            {
                editQuery.Merge(transaction, models.Skip(pageSize * currentDeletePage).Take(pageSize).ToArray());
                currentDeletePage++;
            }
        }

        public static async Task MergeQueryPageSizeAsync<T>(this IEditQuery<T> editQuery, IEnumerable<T> models, int pageSize = 50, ITransaction transaction = null)
              where T : class, IEntity, new()
        {
            int currentDeletePage = 0;
            while (models.Count() > pageSize * currentDeletePage)
            {
                await editQuery.MergeAsync(transaction, models.Skip(pageSize * currentDeletePage).Take(pageSize).ToArray());
                currentDeletePage++;
            }
        }
    }
}
