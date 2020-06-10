﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;

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
                 actionExecutingContext.HttpContext.Request.Method != "GET" &&
                 actionExecutingContext.Controller.GetType().GetCustomAttributes(typeof(SkipValidationAttribute), false).Count() == 0)
            {
                IDictionary<string, object> error = new Dictionary<string, object>();
                error["message"] = "参数验证不通过。";
                error["errors"] = GetValidationSummary(actionExecutingContext.ModelState);
                JsonResult jsonResult = new JsonResult(error);
                jsonResult.StatusCode = StatusCodes.Status400BadRequest;
                actionExecutingContext.Result = jsonResult;
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
}