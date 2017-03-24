using IFramework.Command;
using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.SysExceptions.ErrorCodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace IFramework.AspNet
{
    public static class CommandHttpClient
    {
        public static ApiResult PostJson<T>(this HttpClient apiClient, string requestUri, T value)
        {
            var task = apiClient.PostAsJsonAsync(requestUri, value);
            if (task.Result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return task.Result.Content.ReadAsAsync<ApiResult>().Result;
            }
            else
            {
                return new ApiResult(ErrorCode.HttpStatusError, task.Result.StatusCode.ToString());
            }
        }

        public static ApiResult<TResult> PostJson<T, TResult>(this HttpClient apiClient, string requestUri, T value)
        {
            var task = apiClient.PostAsJsonAsync(requestUri, value);
            if (task.Result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return task.Result.Content.ReadAsAsync<ApiResult<TResult>>().Result;
            }
            else
            {
                return new ApiResult<TResult>(ErrorCode.HttpStatusError, task.Result.StatusCode.ToString());
            }
        }
        //static readonly string CommandActionUrlTemplate = Configuration.GetAppConfig("CommandActionUrlTemplate");    

        public static Task<TResult> DoCommand<TResult>(this HttpClient apiClient, ICommand command, string requestUrl = "api/command")
        {
            return apiClient.PostAsJsonAsync(command, requestUrl)
                            .ContinueWith(t =>
                                t.Result.Content.ReadAsAsync<TResult>()
                            )
                            .Unwrap();
        }


        public static Task<TResult> DoCommand<TResult>(this HttpClient apiClient, ICommand command, TimeSpan timeout, string requestUrl = "api/command")
        {
            return apiClient.PostAsJsonAsync(command, requestUrl)
                            .ContinueWith(t =>
                                t.Result.Content.ReadAsAsync<TResult>()
                             )
                             .Unwrap()
                            .Timeout(timeout);
        }

        public static Task<HttpResponseMessage> DoCommand(this HttpClient apiClient, ICommand command, string requestUrl = "api/command")
        {
            return apiClient.PostAsJsonAsync(command, requestUrl);
        }

        //static string GetCommandUrl(ICommand command)
        //{
        //    return string.Format(CommandActionUrlTemplate, command.GetType().Name);
        //}

        public static Task<HttpResponseMessage> PostAsJsonAsync(this HttpClient client, ICommand command, string requestUrl = "api/command")
        {
            var mediaType = new MediaTypeWithQualityHeaderValue("application/command");
            mediaType.Parameters.Add(new NameValueHeaderValue("command",
                 HttpUtility.UrlEncode(string.Format("{0}, {1}",
                                                command.GetType().FullName,
                                                command.GetType().Assembly.GetName().Name))));
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(client.BaseAddress, requestUrl));
            requestMessage.Content = new ObjectContent(command.GetType(), command, new JsonMediaTypeFormatter(), mediaType);
            //requestMessage.Method = HttpMethod.Post;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client.SendAsync(requestMessage);
        }
    }
}
