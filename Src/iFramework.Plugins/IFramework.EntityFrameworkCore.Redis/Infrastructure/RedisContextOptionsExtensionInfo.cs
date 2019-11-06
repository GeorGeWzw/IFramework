using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IFramework.EntityFrameworkCore.Redis.Infrastructure
{
    public class RedisContextOptionsExtensionInfo: DbContextOptionsExtensionInfo
    {
        private string _logFragment;

        public RedisContextOptionsExtensionInfo(IDbContextOptionsExtension extension) : base(extension)
        {
        }

        public override string LogFragment
        {
            get
            {
                if (this._logFragment == null)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append("StoreName=").Append((this.Extension as RedisOptionsExtension)?.ConnectionMultiplexer).Append(' ');
                    this._logFragment = stringBuilder.ToString();
                }
                return this._logFragment;
            }
        }

        public override long GetServiceProviderHashCode()
        {
            return (this.Extension as RedisOptionsExtension)?.ConnectionMultiplexer?.GetHashCode() ?? 0L;
        }

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            IDictionary<string, string> dictionary = debugInfo;
            int? hashCode = (this.Extension as RedisOptionsExtension)?.ConnectionMultiplexer?.GetHashCode();
            string str = (hashCode.HasValue ? (long) hashCode.GetValueOrDefault() : 0L).ToString((IFormatProvider) CultureInfo.InvariantCulture);
            dictionary["Redis:DatabaseRoot"] = str;
        }

        public override bool IsDatabaseProvider => true;
    }
}
