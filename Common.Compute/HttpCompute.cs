using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Common.Log;
using Common.Model;
using Consul;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace Common.Compute
{
    public class HttpComputeParameter
    {
        public string AssemblyName { get; set; }
        public string AssemblyVersion { get; set; }
        public string ClassName { get; set; }

        [JsonConverter(typeof(JTokenConverter))]
        public JToken Parameter { get; set; }
    }

    public class HttpComputeResult
    {
        public string ResponseEndpoint { get; set; }

        [JsonConverter(typeof(JTokenConverter))]
        public JToken Result { get; set; }
    }

    public static class HttpComputeServiceNameGenerator
    {
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

        private static ConsulClient m_consulClient;
        private static ILogHelper m_logHelper;
        private static CatalogServiceEqualityComparer m_catalogServiceEqualityComparer;

        static HttpTask()
        {
            m_catalogServiceEqualityComparer = new CatalogServiceEqualityComparer();
            m_logHelper = LogHelperFactory.GetDefaultLogHelper();
            ConsulServiceEntity serviceEntity = new ConsulServiceEntity();
            ConfigManager.Configuration.Bind("ConsulService", serviceEntity);
            m_consulClient = new ConsulClient(x => x.Address = new Uri($"http://{serviceEntity.ConsulIP}:{serviceEntity.ConsulPort}"));
        }

        public static ICompute CreateCompute(IHttpClientFactory httpClientFactory)
        {
            return new HttpComputeInstance(httpClientFactory);
        }

        public static IMapReduce CreateMapReduce(IHttpClientFactory httpClientFactory)
        {
            return new HttpMapReduceInstance(httpClientFactory);
        }

        public static IAsyncMapReduce CreateAsyncMapReduce(IHttpClientFactory httpClientFactory)
        {
            return new HttpMapReduceInstance(httpClientFactory);
        }

        private static async Task<IEnumerable<string>> GetServiceEndPoints(Type type)
        {
            string serviceName = HttpComputeServiceNameGenerator.GeneratName(type);
            CatalogService[] catalogServices = (await m_consulClient.Catalog.Service(serviceName)).Response;

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
            return Task.Factory.ContinueWhenAll(tasks.ToArray(), tasks =>
            {
                return tasks.Select(item =>
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

            public HttpComputeInstance(IHttpClientFactory httpClientFactory)
            {
                m_httpClientFactory = httpClientFactory;
            }

            public IEnumerable<TResult> Apply<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, IEnumerable<TParameter> parameters)
            {
                IList<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();
                string[] serviceEndPoints = GetServiceEndPoints(computeFunc.GetType()).Result.ToArray();

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
                string[] serviceEndPoints = (await GetServiceEndPoints(computeFunc.GetType())).ToArray();

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
                string[] serviceEndPoints = GetServiceEndPoints(computeFunc.GetType()).Result.ToArray();

                if (serviceEndPoints.Length == 0)
                {
                    string errorMessage = $"请求错误，无可用计算节点。";
                    m_logHelper.Error("httpCompute", errorMessage);
                    throw new Exception(errorMessage);
                }

                foreach (string serviceEndPoint in serviceEndPoints)
                {
                    tasks.Add(Task.Factory.StartNew((serviceEndPoint) =>
                    {
                        return HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                            m_httpClientFactory, $"http://{serviceEndPoint}/compute/boardcast", httpComputeParameter).Result;
                    }, serviceEndPoint));
                }

                return GetResults<TResult>(tasks);
            }

            public async Task<IEnumerable<TResult>> BordercastAsync<TParameter, TResult>(IComputeFunc<TParameter, TResult> computeFunc, TParameter parameter)
            {
                IList<Task<Task<HttpResponseMessage>>> tasks = new List<Task<Task<HttpResponseMessage>>>();
                HttpComputeParameter httpComputeParameter = CreateParameter(computeFunc, parameter);
                string[] serviceEndPoints = (await GetServiceEndPoints(computeFunc.GetType())).ToArray();

                if (serviceEndPoints.Length == 0)
                {
                    string errorMessage = $"请求错误，无可用计算节点。";
                    await m_logHelper.Error("httpCompute", errorMessage);
                    throw new Exception(errorMessage);
                }

                foreach (string serviceEndPoint in serviceEndPoints)
                {
                    tasks.Add(Task.Factory.StartNew(async (serviceEndPoint) =>
                    {
                        return await HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                            m_httpClientFactory, $"http://{serviceEndPoint}/compute/boardcast", httpComputeParameter);
                    }, serviceEndPoint));
                }

                return await GetResults<TResult>(tasks);
            }

            public IEnumerable<TResult> Call<TResult>(IEnumerable<IComputeFunc<TResult>> computeFuncs)
            {
                IList<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();

                foreach (IComputeFunc<TResult> computeFunc in computeFuncs)
                {
                    string serviceEndPoint = GetServiceEndPoints(computeFunc.GetType()).Result.FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(serviceEndPoint))
                    {
                        string errorMessage = $"请求错误，无可用计算节点。";
                        m_logHelper.Error("httpCompute", errorMessage);
                        throw new Exception(errorMessage);
                    }

                    tasks.Add(Task.Factory.StartNew((serviceEndPoint) =>
                    {
                        return HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                            m_httpClientFactory, $"http://{serviceEndPoint}/compute/call", CreateParameter(computeFunc)).Result;
                    }, serviceEndPoint));
                }

                return GetResults<TResult>(tasks);
            }

            public async Task<IEnumerable<TResult>> CallAsync<TResult>(IEnumerable<IComputeFunc<TResult>> computeFuncs)
            {
                IList<Task<Task<HttpResponseMessage>>> tasks = new List<Task<Task<HttpResponseMessage>>>();

                foreach (IComputeFunc<TResult> computeFunc in computeFuncs)
                {
                    string serviceEndPoint = (await GetServiceEndPoints(computeFunc.GetType())).FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(serviceEndPoint))
                    {
                        string errorMessage = $"请求错误，无可用计算节点。";
                        await m_logHelper.Error("httpCompute", errorMessage);
                        throw new Exception(errorMessage);
                    }

                    tasks.Add(Task.Factory.StartNew(async (serviceEndPoint) =>
                    {
                        return await HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                            m_httpClientFactory, $"http://{serviceEndPoint}/compute/call", CreateParameter(computeFunc));
                    }, serviceEndPoint));
                }

                return await GetResults<TResult>(tasks);
            }
        }

        internal class HttpMapReduceInstance : IMapReduce, IAsyncMapReduce
        {
            private IHttpClientFactory m_httpClientFactory;

            public HttpMapReduceInstance(IHttpClientFactory httpClientFactory)
            {
                m_httpClientFactory = httpClientFactory;
            }

            public TResult Excute<TComputeFunc, TParameter, TResult, TSplitParameter, TSplitResult>
                (IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult> mapReduceTask, TParameter parameter)
            {
                IList<Task<HttpResponseMessage>> tasks = new List<Task<HttpResponseMessage>>();
                string[] serviceEndPoints = GetServiceEndPoints(typeof(TComputeFunc)).Result.ToArray();

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

                    tasks.Add(Task.Factory.StartNew((serviceEndPoint) =>
                    {
                        return HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                            m_httpClientFactory, $"http://{serviceEndPoint}/compute/mapReduce", CreateParameter(mapReduceSplitJob.ComputeFunc, mapReduceSplitJob.Parameter)).Result;
                    }, serviceEndPoint));
                }

                return mapReduceTask.Reduce(GetResults<TSplitResult>(tasks));
            }

            public async Task<TResult> ExcuteAsync<TComputeFunc, TParameter, TResult, TSplitParameter, TSplitResult>
                (IMapReduceTask<TParameter, TResult, TSplitParameter, TSplitResult> mapReduceTask, TParameter parameter)
            {
                IList<Task<Task<HttpResponseMessage>>> tasks = new List<Task<Task<HttpResponseMessage>>>();
                string[] serviceEndPoints = (await GetServiceEndPoints(typeof(TComputeFunc))).ToArray();

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

                    tasks.Add(Task.Factory.StartNew((serviceEndPoint) =>
                    {
                        return HttpJsonHelper.HttpPostByAbsoluteUriAsync(
                            m_httpClientFactory, $"http://{serviceEndPoint}/compute/mapReduce", CreateParameter(mapReduceSplitJob.ComputeFunc, mapReduceSplitJob.Parameter));
                    }, serviceEndPoint));
                }

                return mapReduceTask.Reduce(await GetResults<TSplitResult>(tasks));
            }
        }
    }
}