using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Alloy.HtmlCache
{
    public class RenderTimeFilter : IActionFilter, IResultFilter
    {
        private Stopwatch _watch;

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            _watch = new Stopwatch();
            _watch.Start();
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }

        public void OnResultExecuting(ResultExecutingContext filterContext)
        {

        }

        public void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (_watch != null)
            {
                _watch.Stop();
                var elapsedTime = String.Format("{0}ms", _watch.Elapsed.TotalMilliseconds);

                if (!filterContext.HttpContext.Request.Path.StartsWith("/EPiServer"))
                    filterContext.HttpContext.Response.Write("<p>" + elapsedTime + "</p>");
            }
        }
    }

    public class RenderTimeFilterProvider : IFilterProvider
    {
        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            return new[] { new Filter(new RenderTimeFilter(), FilterScope.Last, 0) };
        }
    }
}