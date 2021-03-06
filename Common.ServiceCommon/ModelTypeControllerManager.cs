using Common.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Common.DAL;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 根据实体动态生成Get，Post，Put，Delete，Search的Controller操作类
    /// </summary>
    public static class ModelTypeControllerManager
    {
        private static ISet<string> m_actionPaths;

        private const string CONTROLLER_SEARCH_TEMPLATE = @"
             namespace {4}.DynamicControllers
             {{
               [Microsoft.AspNetCore.Mvc.Route({0})]
                public class {1}SearchController : Common.ServiceCommon.GenericSearchController<{3}, {2}>
                {{
                    public {1}SearchController(Common.DAL.ISearchQuery<{2}> searchQuery) : base(searchQuery, {5}) {{ }}
                }}
             }}";//search方法

        private const string CONTROLLER_GET_TEMPLATE = @"
             namespace {3}.DynamicControllers
             {{
               [Microsoft.AspNetCore.Mvc.Route({0})]
                public class {1}GetController : Common.ServiceCommon.GenericGetController<{2}>
                {{
                    public {1}GetController(Common.DAL.ISearchQuery<{2}> searchQuery) : base(searchQuery, {4}) {{ }}
                }}
             }}";//get方法

        private const string CONTROLLER_POST_TEMPLATE = @"
             namespace {3}.DynamicControllers
             {{
               [Microsoft.AspNetCore.Mvc.Route({0})]
                public class {1}PostController : Common.ServiceCommon.GenericPostController<{2}>
                {{
                    public {1}PostController(Common.DAL.IEditQuery<{2}> editQuery,Common.ServiceCommon.ISSOUserService ssoUserService) : base(editQuery, ssoUserService, {4}) {{ }}
                }}
             }}";//post方法

        private const string CONTROLLER_PUT_TEMPLATE = @"
             namespace {3}.DynamicControllers
             {{
               [Microsoft.AspNetCore.Mvc.Route({0})]
                public class {1}PutController : Common.ServiceCommon.GenericPutController<{2}>
                {{
                    public {1}PutController(Common.DAL.IEditQuery<{2}> editQuery, Common.DAL.ISearchQuery<{2}> searchQuery,Common.ServiceCommon.ISSOUserService ssoUserService) : base(editQuery, searchQuery, ssoUserService, {4}) {{ }}
                }}
             }}";//put方法

        private const string CONTROLLER_DELETE_TEMPLATE = @"
             namespace {3}.DynamicControllers
             {{
               [Microsoft.AspNetCore.Mvc.Route({0})]
                public class {1}DeleteController : Common.ServiceCommon.GenericDeleteController<{2}>
                {{
                    public {1}DeleteController(Common.DAL.IEditQuery<{2}> editQuery, Common.DAL.ISearchQuery<{2}> searchQuery) : base(editQuery, searchQuery, {4}) {{ }}
                }}
             }}";//delete方法

        static ModelTypeControllerManager()
        {
            m_actionPaths = new HashSet<string>();//初始化

            Type[] controllerTypes = TypeReflector.ReflectType((type) =>//筛选出程序里满足条件的类 不是抽象类不是继承自ControllerBase
            {
                if (!type.GetBaseTypes().Any(type => type == typeof(ControllerBase)) || type.IsAbstract)
                    return false;

                return true;
            });

            for (int i = 0; i < controllerTypes.Length; i++)
            {
                RouteAttribute controllerRouteAttribute = controllerTypes[i].GetCustomAttribute<RouteAttribute>();

                if (controllerRouteAttribute == null)
                    continue;

                string controllerPath = controllerRouteAttribute.Template;

                MethodInfo[] methodInfos = controllerTypes[i].GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                for (int j = 0; j < methodInfos.Length; j++)
                {
                    IEnumerable<HttpGetAttribute> httpGetAttributes = methodInfos[j].GetCustomAttributes<HttpGetAttribute>();
                    HttpPostAttribute httpPostAttribute = methodInfos[j].GetCustomAttribute<HttpPostAttribute>();
                    HttpPutAttribute httpPutAttribute = methodInfos[j].GetCustomAttribute<HttpPutAttribute>();
                    HttpGetAttribute httpDeleteAttribute = methodInfos[j].GetCustomAttribute<HttpGetAttribute>();

                    foreach (HttpGetAttribute httpGetAttribute in httpGetAttributes)
                    {
                        if (!string.IsNullOrWhiteSpace(httpGetAttribute.Template))
                            m_actionPaths.Add($"{controllerPath}/GET/{httpGetAttribute.Template}".ToLower());
                        else
                            m_actionPaths.Add($"{controllerPath}/GET".ToLower());
                    }

                    if (httpPostAttribute != null)
                        m_actionPaths.Add($"{controllerPath}/POST/{httpPostAttribute.Template}".ToLower());

                    if (httpPutAttribute != null)
                        m_actionPaths.Add($"{controllerPath}/PUT/{httpPutAttribute.Template}".ToLower());

                    if (httpDeleteAttribute != null)
                        m_actionPaths.Add($"{controllerPath}/DELETE/{httpDeleteAttribute.Template}".ToLower());
                }
            }
        }

        /// <summary>
        /// 通用实体类转程序集，ModelTypeToAssembly
        /// </summary>
        /// <param name="modelTypes"></param>
        /// <returns></returns>
        public static Assembly GenerateModelTypeControllerToAssembly(Type[] modelTypes)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < modelTypes.Length; i++)
            {
                string actionSearchPath = $"{modelTypes[i].Name}/GET".ToLower();
                string actionGetPath = $"{modelTypes[i].Name}/GET/{{id}}".ToLower();
                string actionPostPath = $"{modelTypes[i].Name}/POST/".ToLower();
                string actionDeletePath = $"{modelTypes[i].Name}/DELETE/{{id}}".ToLower();
                string actionPutPath = $"{modelTypes[i].Name}/PUT/".ToLower();

                IgnoreBuildControllerAttribute ignoreBuildControllerAttribute = modelTypes[i].GetCustomAttribute<IgnoreBuildControllerAttribute>();
                DontSplitSystemAttribute dontSplitSystemAttribute = modelTypes[i].GetCustomAttribute<DontSplitSystemAttribute>();

                if (!m_actionPaths.Contains(actionSearchPath) && !(ignoreBuildControllerAttribute?.IgnoreSearch ?? false))
                {
                    string requestTypeFullName = modelTypes[i].FullName;

                    LinqSearchAttribute linqSearchAttribute = modelTypes[i].GetCustomAttribute<LinqSearchAttribute>();

                    if (linqSearchAttribute != null)
                        requestTypeFullName = linqSearchAttribute.SearchType.FullName;

                    stringBuilder.AppendLine(string.Format(CONTROLLER_SEARCH_TEMPLATE, string.Format("\"{0}\"", modelTypes[i].Name.ToLower()), modelTypes[i].Name, modelTypes[i].FullName,
                                                           requestTypeFullName, AppDomain.CurrentDomain.FriendlyName, (dontSplitSystemAttribute == null).ToString().ToLower()));
                }
                //判断IgnoreBuildControllerAttribute特性是否设置了自动生成接口 并且是否有重复
                if (!m_actionPaths.Contains(actionGetPath) && !(ignoreBuildControllerAttribute?.IgnoreGet ?? false))
                    stringBuilder.AppendLine(string.Format(CONTROLLER_GET_TEMPLATE, string.Format("\"{0}\"", modelTypes[i].Name.ToLower()), modelTypes[i].Name, modelTypes[i].FullName,
                                                           AppDomain.CurrentDomain.FriendlyName, (dontSplitSystemAttribute == null).ToString().ToLower()));

                if (!m_actionPaths.Contains(actionPostPath) && !(ignoreBuildControllerAttribute?.IgnorePost ?? false))
                    stringBuilder.AppendLine(string.Format(CONTROLLER_POST_TEMPLATE, string.Format("\"{0}\"", modelTypes[i].Name.ToLower()), modelTypes[i].Name, modelTypes[i].FullName,
                                                           AppDomain.CurrentDomain.FriendlyName, (dontSplitSystemAttribute == null).ToString().ToLower()));

                if (!m_actionPaths.Contains(actionPutPath) && !(ignoreBuildControllerAttribute?.IgnorePut ?? false))
                    stringBuilder.AppendLine(string.Format(CONTROLLER_PUT_TEMPLATE, string.Format("\"{0}\"", modelTypes[i].Name.ToLower()), modelTypes[i].Name, modelTypes[i].FullName,
                                                           AppDomain.CurrentDomain.FriendlyName, (dontSplitSystemAttribute == null).ToString().ToLower()));

                if (!m_actionPaths.Contains(actionDeletePath) && !(ignoreBuildControllerAttribute?.IgnoreDelete ?? false))
                    stringBuilder.AppendLine(string.Format(CONTROLLER_DELETE_TEMPLATE, string.Format("\"{0}\"", modelTypes[i].Name.ToLower()), modelTypes[i].Name, modelTypes[i].FullName,
                                                           AppDomain.CurrentDomain.FriendlyName, (dontSplitSystemAttribute == null).ToString().ToLower()));
            }

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(stringBuilder.ToString());//生成语法树
            IList<PortableExecutableReference> portableExecutableReferences = new List<PortableExecutableReference>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (assembly.IsDynamic || string.IsNullOrWhiteSpace(assembly.Location))//获取路径是否为空和判断是不是动态生成的
                        continue;

                    portableExecutableReferences.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
                catch
                {
                    continue;
                }
            }

            CSharpCompilation compilation = CSharpCompilation.Create(//生成动态控制器和方法
                                                                     "DynamicController",
                                                                     new[] { syntaxTree },
                                                                     options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).AddReferences(portableExecutableReferences);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                var result = compilation.Emit(memoryStream);

                if (!result.Success)
                    throw new Exception(string.Format("动态编译失败。{0}{1}",
                                                      Environment.NewLine,
                                                      string.Join(Environment.NewLine, result.Diagnostics.
                                                                                              Where(diagnostics => diagnostics.Severity == DiagnosticSeverity.Error).
                                                                                              Select(diagnostics => diagnostics.GetMessage()))));

                memoryStream.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(memoryStream.ToArray());
            }
        }
    }
}