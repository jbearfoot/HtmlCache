using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
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
            //Outcomment to turn on whole page output caching
            //FilterProviders.Providers.Add(new HtmlActionFilterProvider(context.Locate.Advanced.GetInstance<IHtmlCache>(), context.Locate.Advanced.GetInstance<ServiceAccessor<IContentRouteHelper>>()));
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}