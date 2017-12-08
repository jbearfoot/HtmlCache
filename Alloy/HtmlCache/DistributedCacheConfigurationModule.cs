using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using HtmlCache;
using HtmlCache.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Alloy.HtmlCache
{
    [InitializableModule]
    public class DistributedCacheConfigurationModule : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IDistributedCache>(s => new RedisDitributedCache("localhost:6379"));
            //context.Services.AddSingleton<IDistributedCache, NoDistributedHtmlCache>();
            //context.Services.AddSingleton<IDistributedCache, InProcessDistributedCache>();
        }

        public void Initialize(InitializationEngine context)
        {
            FilterProviders.Providers.Add(new RenderTimeFilterProvider());
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}