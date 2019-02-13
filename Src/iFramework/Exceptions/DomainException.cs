﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using IFramework.Event;
using IFramework.Infrastructure;

namespace IFramework.Exceptions
{
    public class ErrorCodeDictionary
    {
        private static readonly Dictionary<object, string> _errorcodeDic = new Dictionary<object, string>();

        public static string GetErrorMessage(object errorcode, params object[] args)
        {
            var errorMessage = _errorcodeDic.TryGetValue(errorcode, string.Empty);
            if (string.IsNullOrEmpty(errorMessage))
            {
                var errorcodeFieldInfo = errorcode.GetType().GetField(errorcode.ToString());
                if (errorcodeFieldInfo != null)
                {
                    errorMessage = errorcodeFieldInfo.GetCustomAttribute<DescriptionAttribute>()?.Description;
                    if (string.IsNullOrEmpty(errorMessage))
                        errorMessage = errorcode.ToString();
                }
            }

            if (args != null && args.Length > 0)
                return string.Format(errorMessage, args);
            return errorMessage;
        }

        public static void AddErrorCodeMessages(IDictionary<object, string> dictionary)
        {
            dictionary.ForEach(p =>
            {
                if (_errorcodeDic.ContainsKey(p.Key))
                    throw new Exception($"ErrorCode dictionary has already had the key {p.Key}");
                _errorcodeDic.Add(p.Key, p.Value);
            });
        }
    }
    [Serializable]
    public class DomainException: Exception
    {
        public IDomainExceptionEvent DomainExceptionEvent { get; protected set; }
        public object ErrorCode { get; protected set; }
        internal string ErrorCodeType { get; set; }
        public DomainException()
        {
            
        }

        public DomainException(IDomainExceptionEvent domainExceptionEvent, Exception innerException = null)
            : this(domainExceptionEvent.ErrorCode, domainExceptionEvent.ToString(), innerException)
        {
            DomainExceptionEvent = domainExceptionEvent;
        }

        public DomainException(object errorCode, string message = null, Exception innerException = null)
            : base(message ?? ErrorCodeDictionary.GetErrorMessage(errorCode), innerException)
        {
            ErrorCode = errorCode;
        }

        public DomainException(object errorCode, object[] args, Exception innerException = null)
            : base(ErrorCodeDictionary.GetErrorMessage(errorCode, args), innerException)
        {
            ErrorCode = errorCode;
        }


        protected DomainException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            ErrorCodeType = (string)info.GetValue(nameof(ErrorCodeType), typeof(string));
            if (ErrorCodeType != null)
            {
                var errorCodeType = Type.GetType(ErrorCodeType);
                if (errorCodeType != null)
                {
                    ErrorCode = info.GetValue(nameof(ErrorCode), errorCodeType);
                }
            }
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ErrorCode), ErrorCode);
            info.AddValue(nameof(ErrorCodeType), ErrorCode?.GetType().GetFullNameWithAssembly());
            base.GetObjectData(info, context);
        }
    }
}