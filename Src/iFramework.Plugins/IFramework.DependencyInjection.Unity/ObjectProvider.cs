﻿using System;
using System.Collections.Generic;
using IFramework.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Unity;
using Unity.Microsoft.DependencyInjection;
using Unity.Resolution;

namespace IFramework.DependencyInjection.Unity
{
    public class ObjectProvider : IObjectProvider
    {
        private UnityContainer _container;

        public ObjectProvider(ObjectProvider parent = null)
        {
            Parent = parent;
        }

        public ObjectProvider(UnityContainer container, ObjectProvider parent = null)
            : this(parent)
        {
            SetComponentContext(container);
        }

        public object GetService(Type serviceType)
        {
            return _container.Resolve(serviceType);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        /// <exception cref="T:System.InvalidOperationException">There is no service of type <typeparamref name="T" />.</exception>
        public object GetRequiredService(Type serviceType)
        {
            return _container.Resolve(serviceType) ?? throw new InvalidOperationException($"There is no service of type {serviceType.Name}");
        }

        public void Dispose()
        {
            _container.Dispose();
        }

        public IObjectProvider Parent { get; }

        public IObjectProvider CreateScope()
        {
            var objectProvider = new ObjectProvider(this);
            var childScope = _container.CreateChildContainer();
            childScope.RegisterInstance<IObjectProvider>(objectProvider);
            objectProvider.SetComponentContext(childScope as UnityContainer);
            return objectProvider;
        }

        public IObjectProvider CreateScope(IServiceCollection serviceCollection)
        {
            var objectProvider = new ObjectProvider(this);
            var childScope = _container.CreateChildContainer();
            childScope.RegisterInstance<IObjectProvider>(objectProvider);
            childScope.BuildServiceProvider(serviceCollection);
            objectProvider.SetComponentContext(childScope as UnityContainer);
            return objectProvider;
        }

        public IObjectProvider CreateScope(Action<IObjectProviderBuilder> buildAction)
        {
            if (buildAction == null)
            {
                throw new ArgumentNullException(nameof(buildAction));
            }

            var objectProvider = new ObjectProvider(this);
            var childScope = _container.CreateChildContainer() as UnityContainer;

            childScope.RegisterInstance<IObjectProvider>(objectProvider);
            var providerBuilder = new ObjectProviderBuilder(childScope);
            buildAction(providerBuilder);

            objectProvider.SetComponentContext(childScope);
            return objectProvider;
        }

        public object GetService(Type t, string name, params Parameter[] parameters)
        {
            return _container.Resolve(t, name, GetResolverOverrides(parameters));
        }

        public object GetService(Type t, params Parameter[] parameters)
        {
            return _container.Resolve(t, GetResolverOverrides(parameters));
        }

        public T GetService<T>(params Parameter[] parameters) where T : class
        {
            return _container.Resolve<T>(GetResolverOverrides(parameters));
        }

        public T GetService<T>(string name, params Parameter[] parameters) where T : class
        {
            return _container.Resolve<T>(name, GetResolverOverrides(parameters));
        }

        public IEnumerable<object> GetAllServices(Type type, params Parameter[] parameters)
        {
            return _container.ResolveAll(type, GetResolverOverrides(parameters));
        }

        public IEnumerable<T> GetAllServices<T>(params Parameter[] parameters) where T : class
        {
            return _container.ResolveAll<T>(GetResolverOverrides(parameters));
        }

        internal void SetComponentContext(UnityContainer container)
        {
            _container = container;
        }

        private ResolverOverride[] GetResolverOverrides(Parameter[] parameters)
        {
            var resolverOverrides = new List<ResolverOverride>();
            parameters.ForEach(parameter => { resolverOverrides.Add(new ParameterOverride(parameter.Name, parameter.Value)); });
            return resolverOverrides.ToArray();
        }
    }
}