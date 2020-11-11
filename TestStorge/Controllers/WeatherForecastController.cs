using Apache.Ignite.Core.Cache.Configuration;
using Common;
using Common.DAL;
using Common.Model;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TestStorge.Controllers
{
    [Route("teststock")]
   public class StockInfoGetController : ControllerBase
    {
        private ISearchQuery<StockInfo> m_searchQuery;
        private IEditQuery<StockInfo> m_editQuery;

        public StockInfoGetController(ISearchQuery<StockInfo> searchQuery,
            IEditQuery<StockInfo> editQuery)
        {
            m_searchQuery = searchQuery;
            m_editQuery = editQuery;
        }

        public IActionResult Get()
        {
            //using (ITransaction transaction = m_editQuery.BeginTransaction())
            //{

            //}

            IEnumerable<StockInfo> search;

            using (var queryable = m_searchQuery.GetQueryable())
            using (var queryable1 = m_searchQuery.GetQueryable())
            {
                var qquerable = from query111 in queryable
                                select query111;

                Thread.Sleep(500);

                search = m_searchQuery.Search(qquerable).ToList();
            }

            return Ok(search);
        }
    }

    public class StockInfo : ViewModelBase
    {

        [LinqToDB.Mapping.Column(Length = 18, CanBeNull = true, Precision = 16, Scale = 2)]
        public decimal? Number { get; set; }
    }
}