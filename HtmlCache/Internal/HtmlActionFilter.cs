using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using HtmlCache.Internal;
using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace HtmlCache.Internal
{
    public class HtmlActionFilter : IActionFilter, IResultFilter
    {
        public const string PagePrefix = "Page:";
        private static object _actionFilterFinishCallbackKey = new object();
        private readonly IHtmlCache _htmlCache;
        private readonly ServiceAccessor<IContentRouteHelper> _contentRouteHelperAccessor;

        public HtmlActionFilter(IHtmlCache htmlCache, ServiceAccessor<IContentRouteHelper> contentRouteHelperAccessor)
        {
            _htmlCache = htmlCache;
            _contentRouteHelperAccessor = contentRouteHelperAccessor;
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var routedContentLink = _contentRouteHelperAccessor().ContentLink;
            if (!filterContext.IsChildAction && !ContentReference.IsNullOrEmpty(routedContentLink))
            {
                var contextResult = _htmlCache.BeginContext(GetCacheKey(routedContentLink, filterContext));
                if (!string.IsNullOrEmpty(contextResult.CachedResult))
                {
                    filterContext.Result = new ContentResult() { Content = contextResult.CachedResult };
                    return;
                }
                contextResult.StartedContext.AddDependency(routedContentLink);
            }
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {
        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
            var currentContext = filterContext.HttpContext.Items[DefaultHtmlCache.ContextKey] as IRenderingContext;
            if (!filterContext.IsChildAction && currentContext != null)
            {
                var response = filterContext.HttpContext.Response;
                response.Filter = new OutputProcessorStream(response.Filter, currentContext, _htmlCache);
            }
        }

        private static void SetChildActionFilterFinishCallback(ControllerContext controllerContext, Action<bool> callback)
        {
            controllerContext.HttpContext.Items[_actionFilterFinishCallbackKey] = callback;
        }

        private string GetCacheKey(ContentReference contentLink, ActionExecutingContext filterContext)
        {
            var hashCodeCombiner = new HashCodeCombiner();
            hashCodeCombiner.Add(contentLink);
            hashCodeCombiner.Add(filterContext.Controller.GetType().ToString());
            hashCodeCombiner.Add(filterContext.HttpContext.Request.RawUrl);
            return $"{PagePrefix}{hashCodeCombiner.CombinedHash}";
        }
    }

    internal class OutputProcessorStream : MemoryStream
    {
        private readonly Stream _stream;
        private readonly IRenderingContext _context;
        private readonly IHtmlCache _htmlCache;

        public OutputProcessorStream(Stream stream, IRenderingContext context, IHtmlCache htmlCache)
        {
            _stream = stream;
            _context = context;
            _htmlCache = htmlCache;
        }

        public string GetHtml()
        {
            Position = 0;
            var sr = new StreamReader(this);
            return sr.ReadToEnd();
        }

        public override void Close()
        {
            if (!_context.PreventCache)
            {
                _htmlCache.CompleteContext(_context, GetHtml());
            }

            Position = 0;
            CopyTo(_stream);
            _stream.Close();
            base.Close();
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (_stream != null)
                    _stream.Dispose();
            }
        }
    }
}
