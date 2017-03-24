using IFramework.SysExceptions;
using IFramework.SysExceptions.ErrorCodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Http.Results;
using System.Net.Http.Headers;
using System.Threading;
using System.Web.Http.Controllers;
using IFramework.Infrastructure;

namespace IFramework.AspNet
{
    public class ApiControllerBase : ApiController
    {

        protected virtual string GetInvalidModelErrorMessage()
        {
            var isDeubg = Config.Configuration.GetCompliationSection()?.Debug ?? false;
            return string.Join(";",
                               ModelState.Where(m => (m.Value?.Errors?.Count ?? 0) > 0)
                                         .Select(m => $"{m.Key}:{(isDeubg ? string.Join(",", m.Value.Errors.Select(e => e.ErrorMessage + e.Exception?.Message)) : "invalid")}"));

        }

        #region process wrapping
        protected ApiResult<T> Process<T>(Func<T> func)
        {
            if (ModelState.IsValid)
            {
                var apiResult = ExceptionManager.Process<T>(func);
                return apiResult;
            }
            else
            {
                return
                    new ApiResult<T>
                    (
                        ErrorCode.InvalidParameters,
                        GetInvalidModelErrorMessage()
                    );
            }
        }
        protected ApiResult Process(Action action)
        {
            if (ModelState.IsValid)
            {
                var apiResult = ExceptionManager.Process(action);
                return apiResult;
            }
            else
            {
                return
                    new ApiResult
                    (
                        ErrorCode.InvalidParameters,
                        GetInvalidModelErrorMessage()
                    );
            }
        }
        List<CookieHeaderValue> _cookies;
        protected void AddCookies(params CookieHeaderValue[]  cookies)
        {
            if (_cookies == null)
            {
                _cookies = new List<CookieHeaderValue>();
            }
            _cookies.AddRange(cookies);
        }
        
        public string TryGetCookie(string key, string defaultValue)
        {
            var value = defaultValue;
            CookieHeaderValue cookie = Request.Headers.GetCookies(key).FirstOrDefault();
            if (cookie != null)
            {
                value = cookie[key].Value;
            }
            return value;
        }

        public void RemoveCookies(string key)
        {
            var cookies = Request.Headers.GetCookies(key).ToList();
            cookies.ForEach(c => c.Expires = DateTimeOffset.Now.AddDays(-1));
            AddCookies(cookies.ToArray());
        }

        public override Task<HttpResponseMessage> ExecuteAsync(HttpControllerContext controllerContext, CancellationToken cancellationToken)
        {
            return base.ExecuteAsync(controllerContext, cancellationToken)
                .ContinueWith(t => {
                    if (_cookies != null && _cookies.Count > 0)
                    {
                        t.Result.Headers.AddCookies(_cookies);
                    }
                    return t.Result;
                }); ;
        }
        

        protected async Task<ApiResult> ProcessAsync(Func<Task> func, bool continueOnCapturedContext = false)
        {
            if (ModelState.IsValid)
            {
                return await ExceptionManager.ProcessAsync(func, continueOnCapturedContext).ConfigureAwait(continueOnCapturedContext);
            }
            else
            {
                return
                    new ApiResult
                    (
                        ErrorCode.InvalidParameters,
                        GetInvalidModelErrorMessage()
                    );
            }
        }

        protected async Task<ApiResult<T>> ProcessAsync<T>(Func<Task<T>> func, bool continueOnCapturedContext = false)
        {
            if (ModelState.IsValid)
            {
                return await ExceptionManager.ProcessAsync(func, continueOnCapturedContext).ConfigureAwait(continueOnCapturedContext);
            }
            else
            {
                return
                   new ApiResult<T>
                   (
                      ErrorCode.InvalidParameters,
                      GetInvalidModelErrorMessage()
                   );
            }

        }

        protected string GetClientIp(HttpRequestMessage request = null)
        {
            request = request ?? Request;

            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }
            else
            {
                return null;
            }
        }
        #endregion
    }
}