using EPiServer.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using System.Web.Mvc;
using System.IO;
using EPiServer.Web.Routing;
using EPiServer.ServiceLocation;
using Microsoft.Extensions.Internal;

namespace HtmlCache.Internal
{
    public class ContentRendererInterceptor : IContentRenderer
    {
        public const string CachePrefix = "Cont";
        private readonly IContentRenderer _defaultRenderer;
        private readonly IHtmlCache _htmlCache;
        private readonly ServiceAccessor<IContentRouteHelper> _contentRouteHelperAccessor;
        private readonly IRenderingContextResolver _contextResolver;

        public ContentRendererInterceptor(IContentRenderer defaultRenderer, IHtmlCache htmlCache, ServiceAccessor<IContentRouteHelper> contentRouteHelperAccessor, IRenderingContextResolver contextResolver)
        {
            _defaultRenderer = defaultRenderer;
            _htmlCache = htmlCache;
            _contentRouteHelperAccessor = contentRouteHelperAccessor;
            _contextResolver = contextResolver;
        }

        public void Render(HtmlHelper helper, PartialRequest partialRequestHandler, IContentData contentData, TemplateModel templateModel)
        {
            var output = _htmlCache.GetOrAdd(CreateCacheKey(contentData, templateModel), context =>
            {
                AddDependecies(context, contentData);
                var buffer = new StringBuilder();
                using (var writer = new StringWriter(buffer))
                {
                    var orgWriter = helper.ViewContext.Writer;
                    try
                    {
                        helper.ViewContext.Writer = writer;
                        _defaultRenderer.Render(helper, partialRequestHandler, contentData, templateModel);

                    }
                    finally
                    {
                        helper.ViewContext.Writer = orgWriter;
                    }
                }

                return buffer.ToString();
            });
            helper.ViewContext.Writer.Write(output);  
        }


        private void AddDependecies(IRenderingContext context, IContentData contentData)
        {
            var routedContent = _contentRouteHelperAccessor().ContentLink;
            if (!ContentReference.IsNullOrEmpty(routedContent))
                context.AddDependency(routedContent);

            var currentContent = contentData as IContent;
            if (currentContent != null && !currentContent.ContentLink.CompareToIgnoreWorkID(routedContent))
                context.AddDependency(currentContent.ContentLink);
        }

        private string CreateCacheKey(IContentData contentData, TemplateModel templateModel)
        {
            var content = contentData as IContent;
            var hashCodeCombiner = new HashCodeCombiner();
            if (content != null)
                hashCodeCombiner.Add(content.ContentLink.ToString());

            hashCodeCombiner.Add(templateModel.Name);
            hashCodeCombiner.Add(templateModel.ModelType);
            hashCodeCombiner.Add(templateModel.TemplateType);
            hashCodeCombiner.Add(templateModel.Tags);
            return $"{CachePrefix}{hashCodeCombiner.CombinedHash}";
        }
    }
}
