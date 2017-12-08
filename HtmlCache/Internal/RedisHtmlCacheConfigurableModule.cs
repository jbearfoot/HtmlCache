using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Text;
using EPiServer.Framework.Initialization;
using EPiServer.Framework;
using EPiServer.Core;
using EPiServer;
using EPiServer.Web.Mvc.Html;
using EPiServer.Web;
using EPiServer.Web.Mvc;
using EPiServer.DataAbstraction;
using EPiServer.Web.Routing;

namespace HtmlCache.Internal
{
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class HtmlCacheConfigurableModule : IConfigurableModule
    {
        private DefaultHtmlCache _htmlCache;
        private IContentEvents _contentEvents;
        private IContentLoader _contentLoader;
        private IContentSecurityRepository _contentSecurityRepository;
        private const string ClearChildrenListingKey = "RedisClearChildrenKey";

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<DefaultHtmlCache>()
                .Forward<DefaultHtmlCache, IHtmlCache>()
                .AddSingleton<IRenderingContextResolver, RenderingContextResolver>()
                .AddSingleton<ServiceAccessor<IContentRouteHelper>>(s => () => s.GetInstance<IContentRouteHelper>());

            context.ConfigurationComplete += (o, e) =>
            {
                e.Services.Intercept<PropertyRenderer>((locator, defaultRenderer) => 
                    new PropertyRendererInterceptor(defaultRenderer,
                        locator.GetInstance<IContextModeResolver>(),
                        locator.GetInstance<IHtmlCache>(),
                        locator.GetInstance<TemplateResolver>(),
                        locator.GetInstance<CachingViewEnginesWrapper>()));

                e.Services.Intercept<ContentAreaRenderer>((locator, defaultRendered) => 
                    new ContentAreaRendererInterceptor(defaultRendered, 
                        locator.GetInstance<IRenderingContextResolver>()));

                e.Services.Intercept<IContentRenderer>((locator, defaultRenderer) =>
                    new ContentRendererInterceptor(defaultRenderer,
                        locator.GetInstance<IHtmlCache>(),
                        locator.GetInstance<ServiceAccessor<IContentRouteHelper>>(),
                        locator.GetInstance<IRenderingContextResolver>()));
            };
        }

        public void Initialize(InitializationEngine context)
        {
            System.Web.Mvc.ViewEngines.Engines.Add(new XhtmlStringViewEngine(new XhtmlStringRenderer(context.Locate.Advanced.GetInstance<IRenderingContextResolver>())));

            _htmlCache = context.Locate.Advanced.GetInstance<DefaultHtmlCache>();
            _contentEvents = context.Locate.Advanced.GetInstance<IContentEvents>();
            _contentSecurityRepository = context.Locate.Advanced.GetInstance<IContentSecurityRepository>();
            _contentLoader = context.Locate.Advanced.GetInstance<IContentLoader>();

            _contentEvents.CreatedContent += ContentCreated;
            _contentEvents.MovedContent += MovedContent;
            _contentEvents.PublishingContent += PublishingContent;
            _contentEvents.PublishedContent += PublishedContent;
            _contentEvents.DeletedContent += DeletedContent;

            _contentSecurityRepository.ContentSecuritySaved += ContentSecuritySaved;
        }

        public void Uninitialize(InitializationEngine context)
        {
            _contentEvents.CreatedContent -= ContentCreated;
            _contentEvents.MovedContent -= MovedContent;
            _contentEvents.PublishingContent -= PublishingContent;
            _contentEvents.PublishedContent -= PublishedContent;
            _contentEvents.DeletedContent -= DeletedContent;

            _contentSecurityRepository.ContentSecuritySaved -= ContentSecuritySaved;
        }

        private void ContentSecuritySaved(object sender, ContentSecurityEventArg e)
        {
            _htmlCache.ContentChanged(e.ContentLink);
            _htmlCache.ChildrenListingChanged(_contentLoader.Get<IContent>(e.ContentLink).ParentLink);
        }

        private void DeletedContent(object sender, DeleteContentEventArgs e)
        {
            _htmlCache.ChildrenListingChanged(ContentReference.IsNullOrEmpty(e.TargetLink) ? e.ContentLink : e.TargetLink);
            foreach (var item in e.DeletedDescendents)
                _htmlCache.ContentChanged(item);
        }

        private void PublishingContent(object sender, ContentEventArgs e)
        {
            var versionable = e.Content as IVersionable;
            if (versionable == null || versionable.IsPendingPublish)
                e.Items[ClearChildrenListingKey] = true;
        }


        private void PublishedContent(object sender, ContentEventArgs e)
        {
            _htmlCache.ContentChanged(e.ContentLink);
            if (e.Items.Contains(ClearChildrenListingKey))
                _htmlCache.ChildrenListingChanged(e.Content.ParentLink);
        }

        private void MovedContent(object sender, ContentEventArgs e)
        {
            _htmlCache.ChildrenListingChanged(e.TargetLink);
            _htmlCache.ChildrenListingChanged((e as MoveContentEventArgs).OriginalParent);
        }

        private void ContentCreated(object sender, ContentEventArgs e)
        {
            _htmlCache.ChildrenListingChanged(e.TargetLink);
        }

      
    }
}
