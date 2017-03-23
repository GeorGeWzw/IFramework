using IFramework.Config;
using IFramework.Infrastructure;
using IFramework.Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace IFramework.AspNet
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Cors;
    using System.Web.Http.Cors;

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class EnableCorsAttribute : Attribute, ICorsPolicyProvider
    {
        private CorsPolicy _corsPolicy;
        private bool _originsValidated;

        public EnableCorsAttribute(string origins, string headers, string methods) : this(origins, headers, methods, false, null)
        {
        }

        public EnableCorsAttribute(string origins, string headers, string methods, bool supportsCredentials = true, string exposedHeaders = null)
        {
            if (string.IsNullOrEmpty(origins))
            {
                origins = Config.Configuration.GetAppConfig("AllowOrigins");
                if (string.IsNullOrEmpty(origins))
                {
                    throw new ArgumentException("ArgumentCannotBeNullOrEmpty", "origins");
                }
            }
            this._corsPolicy = new CorsPolicy();
            this._corsPolicy.SupportsCredentials = supportsCredentials;
            if (origins == "*")
            {
                this._corsPolicy.AllowAnyOrigin = true;
            }
            else
            {
                AddCommaSeparatedValuesToCollection(origins, this._corsPolicy.Origins);
            }
            if (!string.IsNullOrEmpty(headers))
            {
                if (headers == "*")
                {
                    this._corsPolicy.AllowAnyHeader = true;
                }
                else
                {
                    AddCommaSeparatedValuesToCollection(headers, this._corsPolicy.Headers);
                }
            }
            if (!string.IsNullOrEmpty(methods))
            {
                if (methods == "*")
                {
                    this._corsPolicy.AllowAnyMethod = true;
                }
                else
                {
                    AddCommaSeparatedValuesToCollection(methods, this._corsPolicy.Methods);
                }
            }
            if (!string.IsNullOrEmpty(exposedHeaders))
            {
                AddCommaSeparatedValuesToCollection(exposedHeaders, this._corsPolicy.ExposedHeaders);
            }
        }

        private static void AddCommaSeparatedValuesToCollection(string commaSeparatedValues, IList<string> collection)
        {
            string[] strArray = commaSeparatedValues.Split(new char[] { ',' });
            for (int i = 0; i < strArray.Length; i++)
            {
                string str = strArray[i].Trim();
                if (!string.IsNullOrEmpty(str))
                {
                    collection.Add(str);
                }
            }
        }

        public Task<CorsPolicy> GetCorsPolicyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!this._originsValidated)
            {
                ValidateOrigins(this._corsPolicy.Origins);
                this._originsValidated = true;
            }
            return Task.FromResult<CorsPolicy>(this._corsPolicy);
        }

        private static void ValidateOrigins(IList<string> origins)
        {
            foreach (string str in origins)
            {
                if (string.IsNullOrEmpty(str))
                {
                    throw new InvalidOperationException("OriginCannotBeNullOrEmpty");
                }
                if (str.EndsWith("/", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "OriginCannotEndWithSlash", new object[] { str }));
                }
                if (!Uri.IsWellFormedUriString(str, UriKind.Absolute))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "OriginNotWellFormed", new object[] { str }));
                }
                Uri uri = new Uri(str);
                if ((!string.IsNullOrEmpty(uri.AbsolutePath) && !string.Equals(uri.AbsolutePath, "/", StringComparison.Ordinal)) || (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "OriginMustNotContainPathQueryOrFragment", new object[] { str }));
                }
            }
        }

        public IList<string> ExposedHeaders
        {
            get
            {
                return this._corsPolicy.ExposedHeaders;
            }
        }

        public IList<string> Headers
        {
            get
            {
                return this._corsPolicy.Headers;
            }
        }

        public IList<string> Methods
        {
            get
            {
                return this._corsPolicy.Methods;
            }
        }

        public IList<string> Origins
        {
            get
            {
                return this._corsPolicy.Origins;
            }
        }

        public long PreflightMaxAge
        {
            get
            {
                long? preflightMaxAge = this._corsPolicy.PreflightMaxAge;
                if (!preflightMaxAge.HasValue)
                {
                    return -1L;
                }
                return preflightMaxAge.GetValueOrDefault();
            }
            set
            {
                this._corsPolicy.PreflightMaxAge = new long?(value);
            }
        }

        public bool SupportsCredentials
        {
            get
            {
                return this._corsPolicy.SupportsCredentials;
            }
            set
            {
                this._corsPolicy.SupportsCredentials = value;
            }
        }
    }
}