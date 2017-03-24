using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFramework.SysExceptions.ErrorCodes
{
    public class ErrorCode
    {
        public const int NoError = 0;
        public const int HttpStatusError = 0x7ffffffd;
        public const int InvalidParameters = 0x7ffffffe;
        public const int UnknownError = 0x7fffffff;
    }
}
