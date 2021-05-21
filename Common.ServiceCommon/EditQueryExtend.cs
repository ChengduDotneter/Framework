using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.DAL;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 修改对象扩展
    /// </summary>
    public static class EditQueryExtend
    {
        /// <summary>
        /// 事务中分多次删除数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="editQuery">数据修改对象</param>
        /// <param name="ids">要删除的id</param>
        /// <param name="pageSize">每次删除的数据个数</param>
        /// <param name="transaction">执行的事务</param>
        public static void DeleteQueryPageSize<T>(this IEditQuery<T> editQuery, IEnumerable<long> ids, int pageSize = 50, ITransaction transaction = null)
            where T : class, IEntity, new()
        {
            //分页删除 不一次性删除
            int currentDeletePage = 0;
            while (ids.Count() > pageSize * currentDeletePage)
            {
                editQuery.Delete(transaction, ids.Skip(pageSize * currentDeletePage).Take(pageSize).ToArray());
                currentDeletePage++;
            }
        }
        /// <summary>
        /// 事务中异步分批次删除数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="editQuery">修改对象</param>
        /// <param name="ids">要删除的数据id</param>
        /// <param name="pageSize">每次删除的条数</param>
        /// <param name="transaction">执行的事务</param>
        /// <returns></returns>
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
        /// <summary>
        /// 分批多次插入数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="editQuery">数据操作对象</param>
        /// <param name="models">要插入的数据</param>
        /// <param name="pageSize">每次插入的条数</param>
        /// <param name="transaction">执行的事务</param>
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
        /// <summary>
        /// 异步分批次插入数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="editQuery"></param>
        /// <param name="models"></param>
        /// <param name="pageSize"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 分批次合并数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="editQuery"></param>
        /// <param name="models"></param>
        /// <param name="pageSize"></param>
        /// <param name="transaction"></param>
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
        /// <summary>
        /// 异步分批次合并
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="editQuery"></param>
        /// <param name="models"></param>
        /// <param name="pageSize">每次合并的数据数量</param>
        /// <param name="transaction"></param>
        /// <returns></returns>
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
