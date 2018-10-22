using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IFramework.Config;
using IFramework.DependencyInjection;
using IFramework.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace IFramework.Message.Impl
{
    public abstract class HandlerProvider : IHandlerProvider
    {
        public const string FrameworkConfigurationSectionName = nameof(FrameworkConfiguration);
        private readonly ConcurrentDictionary<Type, byte> _discardKeyTypes;
        private readonly ConcurrentDictionary<Type, List<HandlerTypeInfo>> _handlerTypes;

        protected HandlerProvider(params string[] assemblies)
        {
            Assemblies = assemblies;
            _handlerTypes = new ConcurrentDictionary<Type, List<HandlerTypeInfo>>();
            // _HandlerConstuctParametersInfo = new Dictionary<Type, ParameterInfo[]>();
            _discardKeyTypes = new ConcurrentDictionary<Type, byte>();

            RegisterHandlers();
        }
        //protected abstract Type HandlerType { get; }

        private string[] Assemblies { get; }

        //private readonly Dictionary<Type, ParameterInfo[]> _HandlerConstuctParametersInfo;
        protected abstract Type[] HandlerGenericTypes { get; }

        public IList<HandlerTypeInfo> GetHandlerTypes(Type messageType)
        {
            var avaliableHandlerTypes = new List<HandlerTypeInfo>();
            if (_handlerTypes.ContainsKey(messageType))
            {
                var handlerTypes = _handlerTypes[messageType];
                if (handlerTypes != null && handlerTypes.Count > 0)
                {
                    avaliableHandlerTypes = handlerTypes;
                }
            }
            else if (!_discardKeyTypes.ContainsKey(messageType))
            {
                foreach (var handlerTypes in _handlerTypes)
                {
                    if (messageType.IsSubclassOf(handlerTypes.Key))
                    {
                        var messageDispatcherHandlerTypes = _handlerTypes[handlerTypes.Key];
                        if (messageDispatcherHandlerTypes != null && messageDispatcherHandlerTypes.Count > 0)
                        {
                            avaliableHandlerTypes = avaliableHandlerTypes.Union(messageDispatcherHandlerTypes).ToList();
                        }
                    }
                }
                if (avaliableHandlerTypes.Count == 0)
                {
                    _discardKeyTypes.TryAdd(messageType, 0);
                }
                else
                {
                    _handlerTypes.TryAdd(messageType, avaliableHandlerTypes);
                }
            }
            return avaliableHandlerTypes;
        }


        public object GetHandler(Type messageType)
        {
            object handler = null;
            var handlerType = GetHandlerType(messageType);
            if (handlerType != null)
            {
                handler = ObjectProviderFactory.GetService(handlerType.Type);
            }
            return handler;
        }

        public void ClearRegistration()
        {
            _handlerTypes.Clear();
        }

        private void RegisterHandlers()
        {
            var handlers = Configuration.Instance
                                               .GetSection(FrameworkConfigurationSectionName)
                                               ?.Get<FrameworkConfiguration>()
                                               ?.Handlers;
            if (handlers != null)
            {
                foreach (var handlerElement in handlers)
                {
                    if (Assemblies == null || Assemblies.Contains(handlerElement.Name))
                    {
                        switch (handlerElement.SourceType)
                        {
                            case HandlerSourceType.Type:
                                var type = Type.GetType(handlerElement.Source);
                                RegisterHandlerFromType(type);
                                break;
                            case HandlerSourceType.Assembly:
                                var assembly = Assembly.Load(handlerElement.Source);
                                RegisterHandlerFromAssembly(assembly);
                                break;
                        }
                    }
                }
            }

            RegisterInheritedMessageHandlers();
        }

        private void RegisterInheritedMessageHandlers()
        {
            _handlerTypes.Keys.ForEach(messageType =>
                                           _handlerTypes.Keys.Where(type => type.IsSubclassOf(messageType))
                                                        .ForEach(type =>
                                                                 {
                                                                     var list =
                                                                         _handlerTypes[type].Union(_handlerTypes[messageType]).ToList();
                                                                     _handlerTypes[type].Clear();
                                                                     _handlerTypes[type].AddRange(list);
                                                                 }
                                                                ));
        }

        public void Register(Type messageType, Type handlerType)
        {
            var isAsync = false;
            var handleMethod = handlerType.GetMethods()
                                          .FirstOrDefault(m => m.GetParameters()
                                                                .Any(p => p.ParameterType == messageType));
            if (handleMethod != null)
            {
                isAsync = typeof(Task).IsAssignableFrom(handleMethod.ReturnType);
            }
            if (_handlerTypes.ContainsKey(messageType))
            {
                var registeredDispatcherHandlerTypes = _handlerTypes[messageType];
                if (registeredDispatcherHandlerTypes != null)
                {
                    if (!registeredDispatcherHandlerTypes.Exists(ht => ht.Type == handlerType && ht.IsAsync == isAsync))
                    {
                        registeredDispatcherHandlerTypes.Add(new HandlerTypeInfo(handlerType, isAsync));
                    }
                }
                else
                {
                    registeredDispatcherHandlerTypes = new List<HandlerTypeInfo>();
                    _handlerTypes[messageType] = registeredDispatcherHandlerTypes;
                    registeredDispatcherHandlerTypes.Add(new HandlerTypeInfo(handlerType, isAsync));
                }
            }
            else
            {
                var registeredDispatcherHandlerTypes = new List<HandlerTypeInfo>();
                registeredDispatcherHandlerTypes.Add(new HandlerTypeInfo(handlerType, isAsync));
                _handlerTypes.TryAdd(messageType, registeredDispatcherHandlerTypes);
            }
            //var parameterInfoes = handlerType.GetConstructors()
            //                                   .OrderByDescending(c => c.GetParameters().Length)
            //                                   .FirstOrDefault().GetParameters();
            //_HandlerConstuctParametersInfo[handlerType] = parameterInfoes;
        }

        public HandlerTypeInfo GetHandlerType(Type messageType)
        {
            return GetHandlerTypes(messageType).FirstOrDefault();
        }

        #region Private Methods

        private void RegisterHandlerFromAssembly(Assembly assembly)
        {
            var exportedTypes = assembly.GetExportedTypes()
                                        .Where(x => x.IsInterface == false && x.IsAbstract == false
                                                    && x.GetInterfaces()
                                                        .Any(y => y.IsGenericType
                                                                  && HandlerGenericTypes.Contains(y.GetGenericTypeDefinition())));
            foreach (var type in exportedTypes)
            {
                RegisterHandlerFromType(type);
            }
        }

        protected void RegisterHandlerFromType(Type handlerType)
        {
            var ihandlerTypes = handlerType.GetInterfaces()
                                           .Where(x => x.IsGenericType
                                                       && HandlerGenericTypes.Contains(x
                                                                                           .GetGenericTypeDefinition()));
            foreach (var ihandlerType in ihandlerTypes)
            {
                var messageType = ihandlerType.GetGenericArguments().Single();
                //var messageHandlerWrapperType = typeof(MessageHandler<>).MakeGenericType(messageType);
                //var messageHandler = Activator.CreateInstance(handlerType);
                //var messageHandlerWrapper = Activator.CreateInstance(messageHandlerWrapperType, new object[] { messageHandler }) as IMessageHandler;
                Register(messageType, handlerType);
            }
        }

        #endregion
    }
}