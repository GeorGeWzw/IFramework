using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using IFramework.Infrastructure;

using Microsoft.Practices.Unity;
using System.IO;
using System.Xml.Linq;

using Microsoft.Practices.Unity.Configuration;
using System.Configuration;
using IFramework.Command;
using IFramework.Event;
using IFramework.Message;
using System.Web.Configuration;

namespace IFramework.Config
{
    public class Configuration
    {
        public static readonly Configuration Instance = new Configuration();

        public static CompilationSection GetCompliationSection()
        {
            return ConfigurationManager.GetSection("system.web/compilation") as CompilationSection;
        }

        public UnityConfigurationSection UnityConfigurationSection
        {
            get
            {
                return (UnityConfigurationSection)ConfigurationManager.GetSection("unity");
            }
        }

        Configuration()
        {
        }

        public static T GetAppConfig<T>(string key)
        {
            T val = default(T);
            try
            {
                var value = GetAppConfig(key);
                if (typeof(T).IsEquivalentTo(typeof(Guid)))
                {
                    val = (T)Convert.ChangeType(new Guid(value), typeof(T));
                }
                else
                {
                    val = (T)Convert.ChangeType(value, typeof(T));
                }
            }
            catch (Exception)
            {
               
            }
            return val;
        }

        public static string GetAppConfig(string keyname, string configPath = "Config")
        {
            var config = System.Configuration.ConfigurationManager.AppSettings[keyname];
            try
            {
                if (string.IsNullOrWhiteSpace(config))
                {
                    string filePath = Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, configPath);
                    using (TextReader reader = new StreamReader(filePath))
                    {
                        XElement xml = XElement.Load(filePath);
                        if (xml != null)
                        {
                            var element = xml.Elements().SingleOrDefault(e => e.Attribute("key") != null && e.Attribute("key").Value.Equals(keyname));
                            if (element != null)
                            {
                                config = element.Attribute("value").Value;
                            }
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                config = string.Empty;
            }
            return config;
        }



        public Configuration RegisterCommandConsumer(IMessageConsumer commandConsumer, string name)
        {
            if (commandConsumer == null)
            {
                IoCFactory.Resolve<IMessageConsumer>(name);
            }
            else
            {
                IoCFactory.Instance.CurrentContainer
                         .RegisterInstance<IMessageConsumer>(name
                                           , commandConsumer
                                           , new ContainerControlledLifetimeManager());
            }
            return this;
        }

        public Configuration CommandHandlerProviderBuild(ICommandHandlerProvider provider, params string[] assemblies)
        {
            if (provider == null)
            {
                provider = IoCFactory.Resolve<ICommandHandlerProvider>(new ParameterOverride("assemblies", assemblies));
            }
            else
            {
                IoCFactory.Instance.CurrentContainer
                         .RegisterInstance(typeof(ICommandHandlerProvider)
                                           , provider
                                           , new ContainerControlledLifetimeManager());
            }
            return this;
        }


        public Configuration EventSubscriberProviderBuild(IEventSubscriberProvider provider, params string[] assemblies)
        {
            if (provider == null)
            {
                provider = IoCFactory.Resolve<IEventSubscriberProvider>(new ParameterOverride("assemblies", assemblies));
            }
            else
            {
                IoCFactory.Instance.CurrentContainer
                         .RegisterInstance(typeof(IEventSubscriberProvider)
                                           , provider
                                           , new ContainerControlledLifetimeManager());
            }
            return this;
        }

        public Configuration EventBusBuild(params string[] subscriberAssemblies)
        {
            EventSubscriberProviderBuild(null, subscriberAssemblies);
            return this;
        }
    }
}
