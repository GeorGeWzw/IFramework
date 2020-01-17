using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.EntityFrameworkCore.Redis
{
    internal static class DisposableExtensions
    {
        public static ValueTask DisposeAsyncIfAvailable(this IDisposable disposable)
        {
            if (disposable != null)
            {
                if (disposable is IAsyncDisposable asyncDisposable)
                {
                    return asyncDisposable.DisposeAsync();
                }

                disposable.Dispose();
            }

            return default;
        }
    }
}
