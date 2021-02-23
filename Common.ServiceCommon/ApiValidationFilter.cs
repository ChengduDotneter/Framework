using Common.Const;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 跳过验证器的特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class SkipValidationAttribute : Attribute { }

    /// <summary>
    /// Api验证管道
    /// </summary>
    public class ApiValidationFilter : IActionFilter
    {
        /// <summary>
        /// 允许复合验证
        /// </summary>
        public bool AllowMultiple => false;

        /// <summary>
        /// 验证完成后执行
        /// </summary>
        /// <param name="context"></param>
        public void OnActionExecuted(ActionExecutedContext context) { }

        /// <summary>
        /// 验证时执行
        /// </summary>
        /// <param name="actionExecutingContext"></param>
        public void OnActionExecuting(ActionExecutingContext actionExecutingContext)
        {
            if (!actionExecutingContext.ModelState.IsValid &&
                 actionExecutingContext.HttpContext.Request.Method != HttpMethodConst.GET_UPPER &&
                 actionExecutingContext.Controller.GetType().GetCustomAttributes(typeof(SkipValidationAttribute), false).Count() == 0)
            {
                IDictionary<string, object> error = new Dictionary<string, object>();
                error["message"] = "参数验证不通过。";
                error["errors"] = GetValidationSummary(actionExecutingContext.ModelState);

                ValidJsonResult validJsonResult = new ValidJsonResult(error);
                validJsonResult.StatusCode = StatusCodes.Status400BadRequest;
                actionExecutingContext.Result = validJsonResult;
            }
        }

        private static IDictionary<string, object> GetValidationSummary(ModelStateDictionary modelState)
        {
            IDictionary<string, object> error = new Dictionary<string, object>();

            if (!modelState.IsValid)
            {
                foreach (KeyValuePair<string, ModelStateEntry> item in modelState)
                {
                    if (!string.IsNullOrWhiteSpace(item.Key) && item.Value.Errors.Count > 0)
                    {
                        IList<string> errors = new List<string>();
                        error[string.Join(".", item.Key.Split('.').Select(item => JsonUtils.PropertyNameToJavaScriptStyle(item)))] = errors;

                        foreach (ModelError modelError in item.Value.Errors)
                            errors.Add(modelError.ErrorMessage);
                    }
                }
            }

            return error;
        }
    }

    /// <summary>
    /// 验证返回结果
    /// </summary>
    public class ValidJsonResult : JsonResult
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="value"></param>
        public ValidJsonResult(object value) : base(value) { }

        /// <summary>
        /// 执行写入Response
        /// </summary>
        /// <param name="context"></param>
        public override void ExecuteResult(ActionContext context)
        {
            if (string.IsNullOrEmpty(ContentType))
                context.HttpContext.Response.SetJsonContentType();

            context.HttpContext.Response.StatusCode = StatusCode ?? StatusCodes.Status400BadRequest;

            if (Value != null)
                context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(Value)).Wait();
        }

        /// <summary>
        /// 执行异步写入Response
        /// </summary>
        /// <param name="context"></param>
        public override Task ExecuteResultAsync(ActionContext context)
        {
            return Task.Factory.StartNew(async () =>
            {
                if (string.IsNullOrEmpty(ContentType))
                    context.HttpContext.Response.SetJsonContentType();

                context.HttpContext.Response.StatusCode = StatusCode ?? StatusCodes.Status400BadRequest;

                if (Value != null)
                    await context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(Value));
            });
        }
    }
}