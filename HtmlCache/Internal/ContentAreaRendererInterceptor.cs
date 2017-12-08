using EPiServer.Web.Mvc.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPiServer;
using EPiServer.Web;
using EPiServer.Web.Mvc;
using EPiServer.Core;
using System.Web.Mvc;

namespace HtmlCache.Internal
{
    public class ContentAreaRendererInterceptor : ContentAreaRenderer
    {
        private readonly ContentAreaRenderer _defaultRenderer;
        private readonly IRenderingContextResolver _contentRenderingContextResolver;

        public ContentAreaRendererInterceptor(ContentAreaRenderer defaultRenderer, IRenderingContextResolver contentRenderingContextResolver)
        {
            _defaultRenderer = defaultRenderer;
            _contentRenderingContextResolver = contentRenderingContextResolver;
        }

        public override void Render(HtmlHelper htmlHelper, ContentArea contentArea)
        {
            //We do not want to cache the content area if there are any item that is personalized. 
            //An alternative would be to cache the rendering of all non-personalized items but not the personalized items. 
            //We however go with the more simplistic approach to disable caching for the area as whole.
            if (contentArea != null && contentArea.Items.Any(i => (i.AllowedRoles ?? Enumerable.Empty<string>()).Any()))
            {
                var currentContext = _contentRenderingContextResolver.Current;
                if (currentContext != null)
                    currentContext.PreventCache = true;
            }

            _defaultRenderer.Render(htmlHelper, contentArea);
        }
    }
}
