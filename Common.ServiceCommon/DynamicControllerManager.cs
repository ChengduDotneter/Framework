using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 动态控制器配置
    /// </summary>
    public static class DynamicControllerManager
    {
        /// <summary>
        /// 验证
        /// </summary>
        public const string SKIP_VALIDATION = "[Common.ServiceCommon.SkipValidation]";

        private const string DYNAMIC_DATA_TYPE_TEMPLATE = @"
             namespace {2}.DynamicControllers
             {{
                public class {0}
                {{
                    {1}
                }}
             }}";

        private const string DYNAMIC_CONTROLLER_POST_TEMPLATE = @"
              namespace {8}.DynamicControllers
              {{
                [Microsoft.AspNetCore.Mvc.ApiController]
                [Microsoft.AspNetCore.Mvc.Route(""{7}"")]
                {9}
                public class {0}ControllerProxy : Microsoft.AspNetCore.Mvc.ControllerBase
                {{
                    {1}

                    public {0}ControllerProxy({2})
                    {{
                        {3}
                    }}
                    [Microsoft.AspNetCore.Mvc.HttpPost]
                    public Microsoft.AspNetCore.Mvc.IActionResult Post({4})
                    {{
                        return {5}.Post({6});
                    }}
                }}
             }}";

        private const string DYNAMIC_CONTROLLER_PUT_TEMPLATE = @"
              namespace {8}.DynamicControllers
              {{
                [Microsoft.AspNetCore.Mvc.ApiController]
                [Microsoft.AspNetCore.Mvc.Route(""{7}"")]
                {9}
                public class {0}ControllerProxy : Microsoft.AspNetCore.Mvc.ControllerBase
                {{
                    {1}

                    public {0}ControllerProxy({2})
                    {{
                        {3}
                    }}
                    [Microsoft.AspNetCore.Mvc.HttpPut]
                    public Microsoft.AspNetCore.Mvc.IActionResult Put({4})
                    {{
                        return {5}.Put({6});
                    }}
                }}
             }}";

        /// <summary>
        /// 通用动态类转程序集，Type[]ToAssembly
        /// </summary>
        /// <param name="dynamicControllerTypes"></param>
        /// <returns></returns>
        public static Assembly GenerateDynamicControllerToAssembly(Type[] dynamicControllerTypes)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < dynamicControllerTypes.Length; i++)
            {
                //判断该接口类是否继承POSTControllerr基类
                bool isPost = dynamicControllerTypes[i].BaseType.GetGenericTypeDefinition() == typeof(MultipleGenericPostController<,>)
                    || dynamicControllerTypes[i].BaseType.GetGenericTypeDefinition() == typeof(MultipleGenericPostController<,,>)
                    || dynamicControllerTypes[i].BaseType.GetGenericTypeDefinition() == typeof(MultipleGenericPostController<>);

                //判断该接口类是否继承PUTControllerr基类
                bool isPut = dynamicControllerTypes[i].BaseType.GetGenericTypeDefinition() == typeof(MultipleGenericPutController<,>)
                    || dynamicControllerTypes[i].BaseType.GetGenericTypeDefinition() == typeof(MultipleGenericPutController<,,>)
                    || dynamicControllerTypes[i].BaseType.GetGenericTypeDefinition() == typeof(MultipleGenericPutController<>);

                var constructors = dynamicControllerTypes[i].GetConstructors();

                //验证该接口类的私有构造方法是否存在
                if (constructors.Where(item => item.IsPrivate).Count() > 0)
                    throw new Exception($"动态编译失败。{Environment.NewLine}{GetNameByType(dynamicControllerTypes[i])}包含静态构造函数。");

                //验证该接口类的公有构造方法是否为多个
                if (constructors.Where(item => item.IsPublic).Count() > 1)
                    throw new Exception($"动态编译失败。{Environment.NewLine}{GetNameByType(dynamicControllerTypes[i])}包含1个以上的公有构造方法。");

                //验证该接口类的Route
                var routeAttribute = dynamicControllerTypes[i].GetCustomAttribute(typeof(DynamicRouteAttribute));

                if (routeAttribute == null)
                    throw new Exception($"动态编译失败。{Environment.NewLine}{GetNameByType(dynamicControllerTypes[i])}未设置DynamicRoute特性。");

                //动态dto与动态接口基础名称
                var baseName = GetNameByType(dynamicControllerTypes[i]).Replace("Controller", "");

                //动态dto类名
                var dynamicDtoName = $"DynamicData{baseName}";

                //生成动态DTO类型
                string dynamicDataType = string.Format(DYNAMIC_DATA_TYPE_TEMPLATE, dynamicDtoName, GetDynamicDataTypeProperty(dynamicControllerTypes[i], isPost, isPut), AppDomain.CurrentDomain.FriendlyName);

                //获取所需的私有属性
                string dynamicPrivatePropertyString = GetDynamicPrivateProperty(dynamicControllerTypes[i]);

                //获取构造方法参数
                string constructorParametersString = GetConstructorParameter(dynamicControllerTypes[i]);

                //获取构造方法方法体
                string constructorFunctionString = GetConstructorFunction(dynamicControllerTypes[i]);

                //接口执行方法参数
                string functionParametersString = $" { dynamicDtoName } request";

                //接口对象名称
                string controllerObjectString = $"m_{ JsonUtils.PropertyNameToJavaScriptStyle(GetNameByType(dynamicControllerTypes[i]))}";

                //接口对象方法调用参数
                string basefunctionParametersString = GetBasefunctionParametersString(dynamicControllerTypes[i], isPost, isPut);

                //将动态dto加入字符串构造器
                stringBuilder.AppendLine(dynamicDataType);

                //如果接口类型满足以下条件，则将POST接口字符串加入字符串构造器
                if (isPost)
                    stringBuilder.AppendLine(string.Format(DYNAMIC_CONTROLLER_POST_TEMPLATE, baseName, dynamicPrivatePropertyString, constructorParametersString,
                    constructorFunctionString, functionParametersString, controllerObjectString, basefunctionParametersString, ((DynamicRouteAttribute)routeAttribute).Route, AppDomain.CurrentDomain.FriendlyName, dynamicControllerTypes[i].GetCustomAttributes<SkipValidationAttribute>().Count() > 0 ? SKIP_VALIDATION : null));

                //如果接口类型满足以下条件，则将PUT接口字符串加入字符串构造器
                if (isPut)
                    stringBuilder.AppendLine(string.Format(DYNAMIC_CONTROLLER_PUT_TEMPLATE, baseName, dynamicPrivatePropertyString, constructorParametersString,
                    constructorFunctionString, functionParametersString, controllerObjectString, basefunctionParametersString, ((DynamicRouteAttribute)routeAttribute).Route, AppDomain.CurrentDomain.FriendlyName, dynamicControllerTypes[i].GetCustomAttributes<SkipValidationAttribute>().Count() > 0 ? SKIP_VALIDATION : null));
            }

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(stringBuilder.ToString());
            IList<PortableExecutableReference> portableExecutableReferences = new List<PortableExecutableReference>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(assembly.Location))
                        continue;

                    portableExecutableReferences.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
                catch
                {
                    continue;
                }
            }

            CSharpCompilation compilation = CSharpCompilation.Create(
                "DynamicController",
                new[] { syntaxTree },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).
                AddReferences(portableExecutableReferences);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                var result = compilation.Emit(memoryStream);

                if (!result.Success)
                    throw new Exception(string.Format("动态编译失败。{0}{1}",
                                                      Environment.NewLine,
                                                      string.Join(Environment.NewLine, result.Diagnostics
                                                                                             .Where(diagnostics => diagnostics.Severity == DiagnosticSeverity.Error)
                                                                                             .Select(diagnostics => diagnostics.GetMessage()))));

                memoryStream.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(memoryStream.ToArray());
            }
        }

        /// <summary>
        /// 生成动态dto的属性
        /// </summary>
        /// <param name="dynamicControllerType"></param>
        /// <param name="isPost"></param>
        /// <param name="isPut"></param>
        /// <returns></returns>
        private static string GetDynamicDataTypeProperty(Type dynamicControllerType, bool isPost, bool isPut)
        {
            StringBuilder stringBuilder = new StringBuilder();

            MethodInfo methodInfo = null;

            if (isPost)
                methodInfo = dynamicControllerType.GetMethod("DoPost", BindingFlags.Instance | BindingFlags.NonPublic);

            if (isPut)
                methodInfo = dynamicControllerType.GetMethod("DoPut", BindingFlags.Instance | BindingFlags.NonPublic);

            if (methodInfo == null)
                throw new Exception("代码存在问题哦！");

            ParameterInfo[] parameterInfos = methodInfo.GetParameters();

            for (int i = 0; i < parameterInfos?.Length; i++)
            {
                stringBuilder.AppendLine(" [Common.Validation.NotNullAttribute] ");
                stringBuilder.AppendLine($" public {GetFullTypeNameByType(parameterInfos[i].ParameterType)} {JsonUtils.PropertyNameToCSharpStyle(parameterInfos[i].Name)} {{ get;set; }} ");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 生成动态controller的私有属性
        /// </summary>
        /// <param name="dynamicControllerType"></param>
        /// <returns></returns>
        private static string GetDynamicPrivateProperty(Type dynamicControllerType)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($" private {GetFullTypeNameByType(dynamicControllerType)} m_{JsonUtils.PropertyNameToJavaScriptStyle(GetNameByType(dynamicControllerType))} ; ");
            var pubConstrictors = dynamicControllerType.GetConstructors().Where(item => item.IsPublic).ToArray();

            // 动态注入私有属性
            if (pubConstrictors.Length == 1)
            {
                var constructorsParameterTypes = pubConstrictors[0].GetParameters();

                for (int j = 0; j < constructorsParameterTypes.Length; j++)
                {
                    stringBuilder.AppendLine($" private {GetFullTypeNameByType(constructorsParameterTypes[j].ParameterType)} m_{JsonUtils.PropertyNameToJavaScriptStyle(constructorsParameterTypes[j].Name)} ; ");
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 获取动态controller构造方法的参数
        /// </summary>
        /// <param name="dynamicControllerType"></param>
        /// <returns></returns>
        private static string GetConstructorParameter(Type dynamicControllerType)
        {
            StringBuilder stringBuilder = new StringBuilder();
            var pubConstrictors = dynamicControllerType.GetConstructors().Where(item => item.IsPublic).ToArray();

            if (pubConstrictors.Length == 1)
            {
                stringBuilder.Append(string.Join(",", pubConstrictors[0].GetParameters()
                    .Select(item => $"{GetFullTypeNameByType(item.ParameterType)} {JsonUtils.PropertyNameToJavaScriptStyle(item.Name)} ")));
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 获取动态controller的构造函数的方法体
        /// </summary>
        /// <param name="dynamicControllerType"></param>
        /// <returns></returns>
        private static string GetConstructorFunction(Type dynamicControllerType)
        {
            //构造方法中赋值字符串构造器
            StringBuilder stringBuilder = new StringBuilder();
            var constructors = dynamicControllerType.GetConstructors().Where(item => item.IsPublic).ToArray();

            if (constructors.Length == 1)
            {
                var constructorsParameterTypes = constructors[0].GetParameters();

                for (int i = 0; i < constructorsParameterTypes.Length; i++)
                {
                    stringBuilder.AppendLine($" m_{JsonUtils.PropertyNameToJavaScriptStyle(constructorsParameterTypes[i].Name)} = {JsonUtils.PropertyNameToJavaScriptStyle(constructorsParameterTypes[i].Name)} ; ");
                }

                stringBuilder.AppendLine($" m_{JsonUtils.PropertyNameToJavaScriptStyle(GetNameByType(dynamicControllerType))} = new {GetFullTypeNameByType(dynamicControllerType)} ( {string.Join(",", constructorsParameterTypes.Select(item => JsonUtils.PropertyNameToJavaScriptStyle(item.Name)))} ) ;");
            }
            else
            {
                stringBuilder.AppendLine($" m_{JsonUtils.PropertyNameToJavaScriptStyle(GetNameByType(dynamicControllerType))} = new {GetFullTypeNameByType(dynamicControllerType)} ( ) ;");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 获取接口对象方法调用参数
        /// </summary>
        /// <param name="dynamicControllerType"></param>
        /// <param name="isPost"></param>
        /// <param name="isPut"></param>
        /// <returns></returns>
        private static string GetBasefunctionParametersString(Type dynamicControllerType, bool isPost, bool isPut)
        {
            MethodInfo methodInfo = null;

            if (isPost)
                methodInfo = dynamicControllerType.GetMethod("DoPost", BindingFlags.Instance | BindingFlags.NonPublic);

            if (isPut)
                methodInfo = dynamicControllerType.GetMethod("DoPut", BindingFlags.Instance | BindingFlags.NonPublic);

            if (methodInfo == null)
                throw new Exception("代码存在问题哦！");

            ParameterInfo[] parameterInfos = methodInfo.GetParameters();

            return string.Join(",", parameterInfos.Select(item => $" request.{JsonUtils.PropertyNameToCSharpStyle(item.Name)}"));
        }

        /// <summary>
        /// 根据类型获取详细名称（命名空间+名称）
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string GetFullTypeNameByType(Type type)
        {
            if (type.IsGenericType)
                return $" {type.Namespace}.{type.Name.Substring(0, type.Name.IndexOf('`'))}<{string.Join(",", type.GenericTypeArguments.Select(item => GetFullTypeNameByType(item)))}> ";

            return type.FullName;
        }

        /// <summary>
        /// 根据类型属性的名称（泛型以_隔开）
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string GetParameterNameByType(Type type)
        {
            if (type.IsGenericType)
                return $" {string.Join("", type.GenericTypeArguments.Select(item => GetParameterNameByType(item)))}s ";

            return type.Name;
        }

        /// <summary>
        /// 根据类型获取类型的名称
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string GetNameByType(Type type)
        {
            if (type.IsGenericType)
                return GetNameByType(type.GenericTypeArguments.FirstOrDefault());

            return type.Name;
        }
    }
}