using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace HtmlCache.Internal
{
    public class RenderingContextResolver : IRenderingContextResolver
    {
        private readonly ServiceAccessor<HttpContextBase> _requestAccessor;

        public RenderingContextResolver(ServiceAccessor<HttpContextBase> requestAccessor)
        {
            _requestAccessor = requestAccessor;
        }
        public IRenderingContext Current
        {
            get
            {
                var currentRequest = _requestAccessor();
                return currentRequest != null ? currentRequest.Items[DefaultHtmlCache.ContextKey] as IRenderingContext : null;
            }
        }
    }
}
