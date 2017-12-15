using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using HtmlCache.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace HtmlCache
{
    public class HtmlActionFilterProvider : IFilterProvider
    {
        private IHtmlCache _htmlCache;
        private ServiceAccessor<IContentRouteHelper> _contentRouteHelperAccessor;

        public HtmlActionFilterProvider(IHtmlCache htmlCache, ServiceAccessor<IContentRouteHelper> contentRouteHelperAccessor)
        {
            _htmlCache = htmlCache;
            _contentRouteHelperAccessor = contentRouteHelperAccessor;
        }

        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            return new[] { new Filter(new HtmlActionFilter(_htmlCache, _contentRouteHelperAccessor), FilterScope.First, 0) };
        }
    }
}
