using Sample.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EQueueTest
{
    public class ApiResult
    {
        public ErrorCode ErrorCode { get; set; }
        public string Message { get; set; }
    }

    public class ApiResult<TResult> : ApiResult
    {
        public TResult Result { get; set; }
    }

    public static class ExceptionManager
    {
        public static ApiResult Process(Action action)
        {
            try
            {
                action.Invoke();
                return new ApiResult();
            }
            catch (Exception ex)
            {
                var baseException = ex.GetBaseException();
                if (baseException is SysException)
                {
                    var sysException = baseException as SysException;
                    return new ApiResult { ErrorCode = sysException.ErrorCode, Message = sysException.Message };
                }
                else
                {
                    return new ApiResult { ErrorCode = ErrorCode.UnknownError, Message = baseException.Message };
                }
            }
        }

        public static ApiResult<TResult> Process<TResult>(Func<TResult> action)
        {
            try
            {
                var result = action.Invoke();
                return new ApiResult<TResult> { Result = result };
            }
            catch (Exception ex)
            {
                var baseException = ex.GetBaseException();
                if (baseException is SysException)
                {
                    var sysException = baseException as SysException;
                    return new ApiResult<TResult> { ErrorCode = sysException.ErrorCode, Message = sysException.Message };
                }
                else
                {
                    return new ApiResult<TResult> { ErrorCode = ErrorCode.UnknownError, Message = baseException.Message };
                }
            }
        }

    }
}
