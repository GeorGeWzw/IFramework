using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspectCore.DynamicProxy;
using IFramework.Infrastructure;
using MethodInfo = System.Reflection.MethodInfo;

namespace IFramework.DependencyInjection.Autofac
{
    public abstract class InterceptorBase : IInterceptor
    {

        protected virtual object Process(AspectContext context, AspectDelegate next)
        {
            context.Invoke(next);
            return context.ReturnValue;
        }

        private static IEnumerable<InterceptorAttribute> GetInterceptorAttributes(MethodInfo methodInfo)
        {
            return methodInfo?.GetCustomAttributes(typeof(InterceptorAttribute), true).Cast<InterceptorAttribute>() ?? new InterceptorAttribute[0];
        }

        private static IEnumerable<InterceptorAttribute> GetInterceptorAttributes(Type type)
        {
            return type?.GetCustomAttributes(typeof(InterceptorAttribute), true).Cast<InterceptorAttribute>() ?? new InterceptorAttribute[0];
        }

       
        protected Type GetTaskResultType(AspectContext context)
        {
            return context.ImplementationMethod.ReturnType.GetGenericArguments().FirstOrDefault();
        }

        protected static InterceptorAttribute[] GetInterceptorAttributes(AspectContext context)
        {
            return GetInterceptorAttributes(context.ProxyMethod)
                   .Union(GetInterceptorAttributes(context.ProxyMethod.DeclaringType))
                   .Union(GetInterceptorAttributes(context.ImplementationMethod))
                   .Union(GetInterceptorAttributes(context.ImplementationMethod?.DeclaringType))
                   .OrderBy(i => i.Order)
                   .ToArray();
        }

        public virtual Task Invoke(AspectContext context, AspectDelegate next)
        {
            return context.Invoke(next);
        }

        public bool AllowMultiple { get; } = true;
        public bool Inherited { get; set; }
        public int Order { get; set; }
    }

    public class DefaultInterceptor : InterceptorBase
    {
        public virtual async Task InterceptAsync<T>(AspectContext context, AspectDelegate next, InterceptorAttribute[] interceptorAttributes)
        {
            Func<Task<T>> processAsyncFunc = async () =>
            {
                await context.Invoke(next);
                return ((Task<T>)context.ReturnValue).Result;
            };

            foreach (var interceptor in interceptorAttributes)
            {
                var func = processAsyncFunc;
                processAsyncFunc = () => interceptor.ProcessAsync(func,
                                                                  context.ServiceProvider
                                                                         .GetService(typeof(IObjectProvider)) as IObjectProvider,
                                                                  context.Implementation.GetType(),
                                                                  context.Implementation,
                                                                  context.ImplementationMethod,
                                                                  context.Parameters);
            }

            var returnValue = await processAsyncFunc();
            context.ReturnValue = Task.FromResult(returnValue);
        }

        public virtual Task InterceptAsync(AspectContext context, AspectDelegate next, InterceptorAttribute[] interceptorAttributes)
        {
            Func<Task> processAsyncFunc = () => context.Invoke(next);

            foreach (var interceptor in interceptorAttributes)
            {
                var func = processAsyncFunc;
                processAsyncFunc = () => interceptor.ProcessAsync(func,
                                                                  context.ServiceProvider
                                                                         .GetService(typeof(IObjectProvider)) as IObjectProvider,
                                                                  context.Implementation.GetType(),
                                                                  context.Implementation,
                                                                  context.ImplementationMethod,
                                                                  context.Parameters);
            }

            return processAsyncFunc();
        }


     

        public override Task Invoke(AspectContext context, AspectDelegate next)
        {
            var interceptorAttributes = GetInterceptorAttributes(context);

            if (interceptorAttributes.Length > 0)
            {
                var isTaskResult = typeof(Task).IsAssignableFrom(context.ImplementationMethod.ReturnType);
                if (isTaskResult)
                {
                    var resultType = GetTaskResultType(context);
                    if (resultType == null)
                    {
                        return InterceptAsync(context, next, interceptorAttributes);
                    }
                    else
                    {
                        return (Task)this.InvokeGenericMethod(nameof(InterceptAsync),
                                                 new object[] {context, next, interceptorAttributes},
                                                 resultType);
                    }
                }
                else
                {
                    if (context.ImplementationMethod.ReturnType != typeof(void))
                    {
                        Func<dynamic> processFunc = () => Process(context, next);
                        foreach (var interceptor in interceptorAttributes)
                        {
                            var func = processFunc;
                            processFunc = () => interceptor.Process(func,
                                                                    context.ServiceProvider
                                                                           .GetService(typeof(IObjectProvider)) as IObjectProvider,
                                                                    context.Implementation.GetType(),
                                                                    context.Implementation,
                                                                    context.ImplementationMethod,
                                                                    context.Parameters);
                        }

                        context.ReturnValue = processFunc();
                    }
                    else
                    {
                        Action processFunc = () => Process(context, next);
                        foreach (var interceptor in interceptorAttributes)
                        {
                            var func = processFunc;
                            processFunc = () => interceptor.Process(func,
                                                                    context.ServiceProvider
                                                                           .GetService(typeof(IObjectProvider)) as IObjectProvider,
                                                                    context.Implementation.GetType(),
                                                                    context.Implementation,
                                                                    context.ImplementationMethod,
                                                                    context.Parameters);
                        }

                        processFunc();
                    }
                    return Task.CompletedTask;
                }
            }
            else
            {
                return base.Invoke(context, next);
            }
        }
    }
}