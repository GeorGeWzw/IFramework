using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AspectCore.Configuration;
using AspectCore.Extensions.Autofac;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using IFramework.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.DependencyInjection.Autofac
{
    public class ObjectProviderBuilder : IObjectProviderBuilder
    {
        private readonly ContainerBuilder _containerBuilder;

        public ObjectProviderBuilder(ContainerBuilder builder = null)
        {
            _containerBuilder = builder ?? new ContainerBuilder();
            _containerBuilder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(ConfigurationExtension)));
        }

        public ObjectProviderBuilder(IServiceCollection serviceCollection)
            : this()
        {
            Populate(serviceCollection);
        }

        public IObjectProviderBuilder Populate(IServiceCollection serviceCollection)
        {
            _containerBuilder.Populate(serviceCollection);
            return this;
        }

        public IObjectProviderBuilder Register<TFrom>(Func<IObjectProvider, TFrom> implementationFactory, ServiceLifetime lifetime)
        {
            _containerBuilder.Register(componentContext => implementationFactory(new ObjectProvider(componentContext)))
                             .InstanceLifetime(lifetime);
            return this;
        }
        
        public IObjectProvider Build(IServiceCollection serviceCollection = null)
        {
            if (serviceCollection != null)
            {
                Populate(serviceCollection);
            }
           
            _containerBuilder.Register<IObjectProvider>(context =>
                             {
                                 var serviceProvider = context.Resolve<IServiceProvider>();
                                 var componentContextField = serviceProvider.GetType().GetField("_componentContext",
                                                  BindingFlags.NonPublic |
                                                  BindingFlags.Instance);
                                 if (componentContextField?.GetValue(serviceProvider) is IComponentContext componentContext)
                                 {
                                    return new ObjectProvider(componentContext);
                                 }
                                 throw new Exception("Autofac ServiceProvider not exists!");
                             })
                             .InstancePerLifetimeScope();
            var objectProvider = new ObjectProvider();
            objectProvider.SetComponentContext(_containerBuilder.Build());
            return objectProvider;
        }

        public IObjectProviderBuilder RegisterInstance(Type t, object instance)
        {
            _containerBuilder.RegisterInstance(instance)
                             .As(t);
            return this;
            ;
        }

        public IObjectProviderBuilder RegisterInstance(Type t, string name, object instance)
        {
            _containerBuilder.RegisterInstance(instance)
                             .Named(name, t);
            return this;
        }

        public IObjectProviderBuilder RegisterInstance<TInterface>(TInterface instance)
            where TInterface : class
        {
            _containerBuilder.RegisterInstance(instance);
            return this;
        }

        public IObjectProviderBuilder RegisterInstance<TInterface>(string name, TInterface instance)
            where TInterface : class
        {
            _containerBuilder.RegisterInstance(instance)
                             .Named<TInterface>(name);
            return this;
        }


        public IObjectProviderBuilder Register(Type from, Type to, string name = null, params Injection[] injections)
        {
            var injectionMembers = GetInjectionParameters(injections);

            dynamic registrationBuilder;
            if (string.IsNullOrEmpty(name))
            {
                if (to.IsGenericType)
                {
                    registrationBuilder = _containerBuilder.RegisterGeneric(to).As(from);
                }
                else
                {
                    registrationBuilder = _containerBuilder.RegisterType(to).As(from);
                }
            }
            else
            {
                if (to.IsGenericType)
                {
                    registrationBuilder = _containerBuilder.RegisterGeneric(to)
                                                           .Named(name, from)
                                                           .WithParameters(injectionMembers);
                }
                else
                {
                    registrationBuilder = _containerBuilder.RegisterType(to)
                                                           .Named(name, from)
                                                           .WithParameters(injectionMembers);
                }
            }
            RegisterInterceptor(registrationBuilder, from, to, injections);
            return this;
        }

        public IObjectProviderBuilder Register(Type from, Type to, ServiceLifetime lifetime, params Injection[] injections)
        {
            var injectionMembers = GetInjectionParameters(injections);

            dynamic registrationBuilder;
            if (to.IsGenericType)
            {
                registrationBuilder = _containerBuilder.RegisterGeneric(to)
                                                       .As(from)
                                                       .WithParameters(injectionMembers)
                                                       .InstanceLifetime(lifetime);
            }
            else
            {
                registrationBuilder = _containerBuilder.RegisterType(to)
                                                       .As(from)
                                                       .WithParameters(injectionMembers)
                                                       .InstanceLifetime(lifetime);
            }

            RegisterInterceptor(registrationBuilder, from, to, injections);
            return this;
        }

        public IObjectProviderBuilder Register(Type from,
                                               Type to,
                                               string name,
                                               ServiceLifetime lifetime,
                                               params Injection[] injections)
        {
            var injectionMembers = GetInjectionParameters(injections);
            dynamic registrationBuilder;
            if (to.IsGenericType)
            {
                registrationBuilder = _containerBuilder.RegisterGeneric(to)
                                                       .Named(name, from)
                                                       .InstanceLifetime(lifetime)
                                                       .WithParameters(injectionMembers);
            }
            else
            {
                registrationBuilder = _containerBuilder.RegisterType(to)
                                                       .Named(name, from)
                                                       .InstanceLifetime(lifetime)
                                                       .WithParameters(injectionMembers);
            }

            RegisterInterceptor(registrationBuilder, from, to, injections);
            return this;
        }

        public IObjectProviderBuilder Register<TFrom, TTo>(ServiceLifetime lifetime, params Injection[] injections)
            where TFrom : class where TTo : class, TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            var toType = typeof(TTo);
            var fromType = typeof(TFrom);
            dynamic registrationBuilder;
            if (toType.IsGenericType)
            {
                registrationBuilder = _containerBuilder.RegisterGeneric(toType)
                                                       .As(fromType)
                                                       .InstanceLifetime(lifetime)
                                                       .WithParameters(injectionMembers);
            }
            else
            {
                registrationBuilder = _containerBuilder.RegisterType(toType)
                                                       .As(fromType)
                                                       .InstanceLifetime(lifetime)
                                                       .WithParameters(injectionMembers);
            }

            RegisterInterceptor(registrationBuilder,  typeof(TFrom), typeof(TTo), injections);
            return this;
        }

        public IObjectProviderBuilder Register<TFrom, TTo>(params Injection[] injections)
            where TFrom : class where TTo : class, TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            dynamic registrationBuilder;
            var toType = typeof(TTo);
            var fromType = typeof(TFrom);
            if (toType.IsGenericType)
            {
                registrationBuilder = _containerBuilder.RegisterGeneric(toType)
                                                       .As(fromType)
                                                       .WithParameters(injectionMembers);
            }
            else
            {
                registrationBuilder = _containerBuilder.RegisterType(toType)
                                                       .As(fromType)
                                                       .WithParameters(injectionMembers);
            }

            RegisterInterceptor(registrationBuilder,  typeof(TFrom), typeof(TTo), injections);
            return this;
        }

        public IObjectProviderBuilder Register<TFrom, TTo>(string name, params Injection[] injections)
            where TFrom : class where TTo : class, TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            dynamic registrationBuilder;
            var toType = typeof(TTo);
            var fromType = typeof(TFrom);
            if (toType.IsGenericType)
            {
                registrationBuilder = _containerBuilder.RegisterGeneric(toType)
                                                       .Named(name, fromType)
                                                       .WithParameters(injectionMembers);
            }
            else
            {
                registrationBuilder = _containerBuilder.RegisterType(toType)
                                                       .Named(name, fromType)
                                                       .WithParameters(injectionMembers);
            }

            RegisterInterceptor(registrationBuilder, typeof(TFrom), typeof(TTo), injections);
            return this;
        }

        public IObjectProviderBuilder Register<TFrom, TTo>(string name, ServiceLifetime lifetime, params Injection[] injections)
            where TFrom : class where TTo : class, TFrom
        {
            var injectionMembers = GetInjectionParameters(injections);
            dynamic registrationBuilder;
            var toType = typeof(TTo);
            var fromType = typeof(TFrom);
            if (toType.IsGenericType)
            {
                registrationBuilder = _containerBuilder.RegisterGeneric(toType)
                                                       .Named(name, fromType)
                                                       .InstanceLifetime(lifetime)
                                                       .WithParameters(injectionMembers);
            }
            else
            {
                registrationBuilder = _containerBuilder.RegisterType(toType)
                                                       .Named(name, fromType)
                                                       .InstanceLifetime(lifetime)
                                                       .WithParameters(injectionMembers);
            }

       
            RegisterInterceptor(registrationBuilder, typeof(TFrom), typeof(TTo), injections);
            return this;
        }
      
         private ConcurrentDictionary<Type, List<Type>> _interceptorMapping = new ConcurrentDictionary<Type, List<Type>>();

        private void RegisterInterceptor(dynamic registrationBuilder, Type typeFrom, Type typeTo, Injection[] injections)
        {
            injections.ForEach(injection =>
            {
                //if (injection is InterfaceInterceptorInjection)
                //{
                //    RegistrationExtensions.EnableInterfaceInterceptors(registrationBuilder);
                //}
                //else if (injection is VirtualMethodInterceptorInjection)
                //{
                //    RegistrationExtensions.EnableClassInterceptors(registrationBuilder);
                //}
                //else if (injection is TransparentProxyInterceptorInjection)
                //{
                //    throw new NotImplementedException();
                   
                //}
                //else 
                if (injection is InterceptionBehaviorInjection behaviorInjection)
                {
                    var interceptorType = behaviorInjection.BehaviorType ?? typeof(DefaultInterceptor);
                    var typeList = _interceptorMapping.GetOrAdd(interceptorType, key => new List<Type>());
                    if (!typeList.Contains(typeFrom))
                    {
                        typeList.Add(typeFrom);
                    }

                    _containerBuilder.RegisterDynamicProxy(config => config.Interceptors
                                                                           .AddTyped(interceptorType,
                                                                                     method =>
                                                                                     {
                                                                                         return typeList.Contains(method.DeclaringType);
                                                                                     }));

                    //RegistrationExtensions.InterceptedBy(registrationBuilder, interceptorType);
                }
            });
        }

        private IEnumerable<global::Autofac.Core.Parameter> GetInjectionParameters(Injection[] injections)
        {
            var injectionMembers = new List<global::Autofac.Core.Parameter>();
            injections.ForEach(injection =>
            {
                if (injection is ConstructInjection)
                {
                    var constructInjection = injection as ConstructInjection;
                    injectionMembers.AddRange(constructInjection.Parameters
                                                                .Select(p => new NamedParameter(p.ParameterName, p.ParameterValue)));
                }
                else if (injection is ParameterInjection)
                {
                    var propertyInjection = injection as ParameterInjection;
                    injectionMembers.Add(new NamedPropertyParameter(propertyInjection.ParameterName,
                                                                    propertyInjection.ParameterValue));
                }
            });
            return injectionMembers;
        }
    }
}