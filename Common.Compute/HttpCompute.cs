using Common.Log;
using Common.Model;
using Consul;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Common.Compute
{
    /// <summary>
    /// HTTP并行计算参数
    /// </summary>
    public class HttpComputeParameter
    {
        /// <summary>
        /// 程序集名称
        /// </summary>
        public string AssemblyName { get; set; }

        /// <summary>
        /// 程序集版本
        /// </summary>
        public string AssemblyVersion { get; set; }

        /// <summary>
        /// 类名
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// 请求参数
        /// </summary>
        [JsonConverter(typeof(JTokenConverter))]
        public JToken Parameter { get; set; }
    }

    /// <summary>
    /// HTTP并行计算结果
    /// </summary>
    public class HttpComputeResult
    {
        /// <summary>
        /// 结果返回节点
        /// </summary>
        public string ResponseEndpoint { get; set; }

        /// <summary>
        /// 返回结果
        /// </summary>
        [JsonConverter(typeof(JTokenConverter))]
        public JToken Result { get; set; }
    }

    /// <summary>
    /// 并行计算服务名称构建器
    /// </summary>
    public static class HttpComputeServiceNameGenerator
    {
        /// <summary>
        /// 根据类型构建并行计算名称
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GeneratName(Type type)
        {
            return $"compute_service: {type.FullName}";
        }
    }

    internal static class HttpTask
    {
        private class CatalogServiceEqualityComparer : IEqualityComparer<CatalogService>
        {
            public bool Equals(CatalogService x, CatalogService y)
            {
                return x.Address == y.Address && x.ServicePort == y.ServicePort;
            }

            public int GetHashCode(CatalogService obj)
            {
                return obj.Address.GetHashCode() ^ obj.ServicePort.GetHashCode();
            }
        }

        private static ILogHelper m_logHelper;
        private static CatalogServiceEqualityComparer m_catalogServiceEqualityComparer;

        static HttpTask()
        {
            m_catalogServiceEqualityComparer = new CatalogServiceEqualityComparer();
            m_logHelper = LogHelperFactory.GetDefaultLogHelper();
        }

        public static ICompute CreateCompute(IHttpClientFactory httpClientFactory, ConsulServiceEntity consulServiceEntity)
        {
            return new HttpComputeInstance(httpClientFactory, consulServiceEntity);
        }

        public static IMapReduce CreateMapReduce(IHttpClientFactory httpClientFactory, ConsulServiceEntity consulServiceEntity)
        {
            return new HttpMapReduceInstance(httpClientFactory, consulServiceEntity);
        }

        public static IAsyncMapReduce CreateAsyncMapReduce(IHttpClientFactory httpClientFactory, ConsulServiceEntity consulServiceEntity)
        {
            return new HttpMapReduceInstance(httpClientFactory, consulServiceEntity);
        }

        private static async Task<IEnumerable<string>> GetServiceEndPoints(Type type, ConsulServiceEntity consulServiceEntity)
        {
            using ConsulClient consulClient = new ConsulClient(x => x.Address = new Uri($"http://{consulServiceEntity.ConsulIP}:{consulServiceEntity.ConsulPort}"));
            string serviceName = HttpComputeServiceNameGenerator.GeneratName(type);
            CatalogService[] catalogServices = (await consulClient.Catalog.Service(serviceName)).Response;

            return catalogServices.Distinct(m_catalogServiceEqualityComparer).Select(item => $"{item.ServiceAddress}:{item.ServicePort}");
        }

        private static HttpComputeParameter CreateParameter<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, TParameter parameter)
        {
            return new HttpComputeParameter()
            {
                AssemblyName = computeFunc.GetType().Assembly.GetName().FullName,
                AssemblyVersion = computeFunc.GetType().Assembly.GetName().Version.ToString(),
                ClassName = computeFunc.GetType().FullName,
                Parameter = JObject.FromObject(parameter)
            };
        }

        private static HttpComputeParameter CreateParameter<TResult>(IComputeFunc<TResult> computeFunc)
        {
            return new HttpComputeParameter()
            {
                AssemblyName = computeFunc.GetType().Assembly.GetName().FullName,
                AssemblyVersion = computeFunc.GetType().Assembly.GetName().Version.ToString(),
                ClassName = computeFunc.GetType().FullName
            };
        }

        private static IEnumerable<TResult> GetResults<TResult>(IEnumerable<Task<HttpResponseMessage>> tasks)
        {
            return Task.Factory.ContinueWhenAll(tasks.ToArray(), items =>
            {
                return items.Select(item =>
                {
                    if (item.Result.StatusCode != HttpStatusCode.OK)
                    {
                        string errorMessage = $"{item.AsyncState}请求错误，{item.Result.Content.ReadAsStringAsync()}";
                        m_logHelper.Error("httpCompute", errorMessage);
                        throw new Exception(errorMessage);
                    }

                    return JObject.Parse(item.Result.Content.ReadAsStringAsync().Result).ToObject<HttpComputeResult>().Result.ToObject<TResult>();
                });
            }).Result;
        }

        private static async Task<IEnumerable<TResult>> GetResults<TResult>(IEnumerable<Task<Task<HttpResponseMessage>>> tasks)
        {
            return await Task.WhenAll(tasks.Select(async item =>
            {
                if ((await item.Result).StatusCode != HttpStatusCode.OK)
                {
                    string errorMessage = $"{item.AsyncState}请求错误，{(await item.Result).Content.ReadAsStringAsync()}";
                    await m_logHelper.Error("httpCompute", errorMessage);
                    throw new Exception(errorMessage);
                }

                return JObject.Parse(await (await item.Result).Content.ReadAsStringAsync()).ToObject<HttpComputeResult>().Result.ToObject<TResult>();
            }));
        }

        internal class HttpComputeInstance : ICompute
        {
            private IHttpClientFactory m_httpClientFactory;
            private ConsulServiceEntity m_consulServiceEntity;

            public HttpComputeInstance(IHttpClientFactory httpClientFactory, ConsulServiceEntity consulServiceEntity)
            {
                m_httpClientFactory = httpClientFactory;
                m_consulServiceEntity = consulServiceEntity;
            }

            public IEnumerable<TResult> Apply<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, IEnumerable<TParameter> parameters)
            {
                IList<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();
                string[] serviceEndPoints = GetServiceEndPoints(computeFunc.GetType(), m_consulServiceEntity).Result.ToArray();

                if (serviceEndPoints.Length == 0)
                {
                    string errorMessage = $"请求错误，无可用计算节点。";
                    m_logHelper.Error("httpCompute", errorMessage);
                    throw new Exception(errorMessage);
                }

                int index = 0;

                foreach (TParameter parameter in parameters)
                {
                    tasks.Add(Task.Factory.StartNew((serviceEndPoint) =>
                    {
                        HttpComputeParameter httpComputeParameter = CreateParameter(computeFunc, parameter);
                        return HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                                                                         m_httpClientFactory, $"http://{serviceEndPoint}/compute/apply", httpComputeParameter).Result;
                    }, serviceEndPoints[index % serviceEndPoints.Length]));

                    index++;
                }

                return GetResults<TResult>(tasks);
            }

            public async Task<IEnumerable<TResult>> ApplyAsync<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, IEnumerable<TParameter> parameters)
            {
                IList<Task<Task<HttpResponseMessage>>> tasks = new List<Task<Task<HttpResponseMessage>>>();
                string[] serviceEndPoints = (await GetServiceEndPoints(computeFunc.GetType(), m_consulServiceEntity)).ToArray();

                if (serviceEndPoints.Length == 0)
                {
                    string errorMessage = $"请求错误，无可用计算节点。";
                    await m_logHelper.Error("httpCompute", errorMessage);
                    throw new Exception(errorMessage);
                }

                int index = 0;

                foreach (TParameter parameter in parameters)
                {
                    tasks.Add(Task.Factory.StartNew(async (serviceEndPoint) =>
                    {
                        HttpComputeParameter httpComputeParameter = CreateParameter(computeFunc, parameter);
                        return await HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                                                                               m_httpClientFactory, $"http://{serviceEndPoint}/compute/apply", httpComputeParameter);
                    }, serviceEndPoints[index % serviceEndPoints.Length]));

                    index++;
                }

                return await GetResults<TResult>(tasks);
            }

            public IEnumerable<TResult> Bordercast<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, TParameter parameter)
            {
                IList<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();
                HttpComputeParameter httpComputeParameter = CreateParameter(computeFunc, parameter);
                string[] serviceEndPoints = GetServiceEndPoints(computeFunc.GetType(), m_consulServiceEntity).Result.ToArray();

                if (serviceEndPoints.Length == 0)
                {
                    string errorMessage = $"请求错误，无可用计算节点。";
                    m_logHelper.Error("httpCompute", errorMessage);
                    throw new Exception(errorMessage);
                }

                foreach (string serviceEndPoint in serviceEndPoints)
                {
                    tasks.Add(Task.Factory.StartNew((item) =>
                    {
                        return HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                                                                         m_httpClientFactory, $"http://{item}/compute/boardcast", httpComputeParameter).Result;
                    }, serviceEndPoint));
                }

                return GetResults<TResult>(tasks);
            }

            public async Task<IEnumerable<TResult>> BordercastAsync<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, TParameter parameter)
            {
                IList<Task<Task<HttpResponseMessage>>> tasks = new List<Task<Task<HttpResponseMessage>>>();
                HttpComputeParameter httpComputeParameter = CreateParameter(computeFunc, parameter);
                string[] serviceEndPoints = (await GetServiceEndPoints(computeFunc.GetType(), m_consulServiceEntity)).ToArray();

                if (serviceEndPoints.Length == 0)
                {
                    string errorMessage = $"请求错误，无可用计算节点。";
                    await m_logHelper.Error("httpCompute", errorMessage);
                    throw new Exception(errorMessage);
                }

                foreach (string serviceEndPoint in serviceEndPoints)
                {
                    tasks.Add(Task.Factory.StartNew(async (item) =>
                    {
                        return await HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                                                                               m_httpClientFactory, $"http://{item}/compute/boardcast", httpComputeParameter);
                    }, serviceEndPoint));
                }

                return await GetResults<TResult>(tasks);
            }

            public IEnumerable<TResult> Call<TResult>(IEnumerable<IComputeFunc<TResult>> computeFuncs)
            {
                IList<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();

                foreach (IComputeFunc<TResult> computeFunc in computeFuncs)
                {
                    string serviceEndPoint = GetServiceEndPoints(computeFunc.GetType(), m_consulServiceEntity).Result.FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(serviceEndPoint))
                    {
                        string errorMessage = $"请求错误，无可用计算节点。";
                        m_logHelper.Error("httpCompute", errorMessage);
                        throw new Exception(errorMessage);
                    }

                    tasks.Add(Task.Factory.StartNew((item) =>
                    {
                        return HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                                                                         m_httpClientFactory, $"http://{item}/compute/call", CreateParameter(computeFunc)).Result;
                    }, serviceEndPoint));
                }

                return GetResults<TResult>(tasks);
            }

            public async Task<IEnumerable<TResult>> CallAsync<TResult>(IEnumerable<IComputeFunc<TResult>> computeFuncs)
            {
                IList<Task<Task<HttpResponseMessage>>> tasks = new List<Task<Task<HttpResponseMessage>>>();

                foreach (IComputeFunc<TResult> computeFunc in computeFuncs)
                {
                    string serviceEndPoint = (await GetServiceEndPoints(computeFunc.GetType(), m_consulServiceEntity)).FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(serviceEndPoint))
                    {
                        string errorMessage = $"请求错误，无可用计算节点。";
                        await m_logHelper.Error("httpCompute", errorMessage);
                        throw new Exception(errorMessage);
                    }

                    tasks.Add(Task.Factory.StartNew(async (item) =>
                    {
                        return await HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                                                                               m_httpClientFactory, $"http://{item}/compute/call", CreateParameter(computeFunc));
                    }, serviceEndPoint));
                }

                return await GetResults<TResult>(tasks);
            }
        }

        internal class HttpMapReduceInstance : IMapReduce, IAsyncMapReduce
        {
            private IHttpClientFactory m_httpClientFactory;
            private ConsulServiceEntity m_consulServiceEntity;

            public HttpMapReduceInstance(IHttpClientFactory httpClientFactory, ConsulServiceEntity consulServiceEntity)
            {
                m_httpClientFactory = httpClientFactory;
                m_consulServiceEntity = consulServiceEntity;
            }

            public TResult Excute<TComputeFunc, TParameter, TResult, TSplitParameter, TSplitResult>(IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult> mapReduceTask, TParameter parameter)
            {
                IList<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();
                string[] serviceEndPoints = GetServiceEndPoints(typeof(TComputeFunc), m_consulServiceEntity).Result.ToArray();

                if (serviceEndPoints.Length == 0)
                {
                    string errorMessage = $"请求错误，无可用计算节点。";
                    m_logHelper.Error("httpCompute", errorMessage);
                    throw new Exception(errorMessage);
                }

                IEnumerable<MapReduceSplitJob<TSplitParameter, TSplitResult>> mapReduceSplitJobs = mapReduceTask.Split(serviceEndPoints.Length, parameter);
                IEnumerable<Tuple<MapReduceSplitJob<TSplitParameter, TSplitResult>, string>> tuples = mapReduceSplitJobs.Zip(serviceEndPoints, (mapReduceSplitJob, serviceEndPoint) =>
                {
                    if (mapReduceSplitJob.ComputeFunc.GetType() != typeof(TComputeFunc))
                    {
                        string errorMessage = $"请求错误，MapReduceSplitJob的ComputeFunc类型与传入的ComputeFunc类型不匹配。";
                        m_logHelper.Error("httpCompute", errorMessage);
                        throw new Exception(errorMessage);
                    }

                    return Tuple.Create(mapReduceSplitJob, serviceEndPoint);
                });

                foreach (Tuple<MapReduceSplitJob<TSplitParameter, TSplitResult>, string> tuple in tuples)
                {
                    (MapReduceSplitJob<TSplitParameter, TSplitResult> mapReduceSplitJob, string serviceEndPoint) = tuple;

                    tasks.Add(Task.Factory.StartNew((item) =>
                    {
                        return HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                                                                         m_httpClientFactory, $"http://{item}/compute/mapReduce", CreateParameter(mapReduceSplitJob.ComputeFunc, mapReduceSplitJob.Parameter)).Result;
                    }, serviceEndPoint));
                }

                return mapReduceTask.Reduce(GetResults<TSplitResult>(tasks));
            }

            public async Task<TResult> ExcuteAsync<TComputeFunc, TParameter, TResult, TSplitParameter, TSplitResult>(IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult> mapReduceTask, TParameter parameter)
            {
                IList<Task<Task<HttpResponseMessage>>> tasks = new List<Task<Task<HttpResponseMessage>>>();
                string[] serviceEndPoints = (await GetServiceEndPoints(typeof(TComputeFunc), m_consulServiceEntity)).ToArray();

                if (serviceEndPoints.Length == 0)
                {
                    string errorMessage = $"请求错误，无可用计算节点。";
                    await m_logHelper.Error("httpCompute", errorMessage);
                    throw new Exception(errorMessage);
                }

                IEnumerable<MapReduceSplitJob<TSplitParameter, TSplitResult>> mapReduceSplitJobs = mapReduceTask.Split(serviceEndPoints.Length, parameter);
                IEnumerable<Tuple<MapReduceSplitJob<TSplitParameter, TSplitResult>, string>> tuples = mapReduceSplitJobs.Zip(serviceEndPoints, (mapReduceSplitJob, serviceEndPoint) =>
                {
                    if (mapReduceSplitJob.ComputeFunc.GetType() != typeof(TComputeFunc))
                    {
                        string errorMessage = $"请求错误，MapReduceSplitJob的ComputeFunc类型与传入的ComputeFunc类型不匹配。";
                        m_logHelper.Error("httpCompute", errorMessage);
                        throw new Exception(errorMessage);
                    }

                    return Tuple.Create(mapReduceSplitJob, serviceEndPoint);
                });

                foreach (Tuple<MapReduceSplitJob<TSplitParameter, TSplitResult>, string> tuple in tuples)
                {
                    (MapReduceSplitJob<TSplitParameter, TSplitResult> mapReduceSplitJob, string serviceEndPoint) = tuple;

                    tasks.Add(Task.Factory.StartNew((item) =>
                    {
                        return HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                                                                         m_httpClientFactory, $"http://{item}/compute/mapReduce", CreateParameter(mapReduceSplitJob.ComputeFunc, mapReduceSplitJob.Parameter));
                    }, serviceEndPoint));
                }

                return mapReduceTask.Reduce(await GetResults<TSplitResult>(tasks));
            }
        }
    }
}