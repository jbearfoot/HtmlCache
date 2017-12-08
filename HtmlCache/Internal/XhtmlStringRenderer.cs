using EPiServer.Core;
using EPiServer.Framework.DataAnnotations;
using EPiServer.Security;
using EPiServer.Web;
using EPiServer.Web.Mvc.Html;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace HtmlCache.Internal
{
    public class XhtmlStringRenderer : IView
    {
        private readonly IRenderingContextResolver _contentRenderingContextResolver;

        public XhtmlStringRenderer(IRenderingContextResolver contentRenderingContextResolver)
        {
            _contentRenderingContextResolver = contentRenderingContextResolver;
        }

        public void Render(ViewContext viewContext, TextWriter writer)
        {
            var xhtmlString = viewContext.ViewData.Model as XhtmlString;
            if (xhtmlString != null)
            {
                var roleSecurityDescriptors = xhtmlString.Fragments
                    .OfType<ISecurable>()
                    .Select(s => s.GetSecurityDescriptor())
                    .OfType<IRoleSecurityDescriptor>();

                //We do not want to cache the xhtmlstring if there are any fragments that is personalized. 
                //An alternative would be to cache the rendering of all non-personalized fragments but not the personalized fragments. 
                //We however go with the more simplistic approach and disable cache for whole xhtmlstring
                if (roleSecurityDescriptors.Any(rs => (rs.RoleIdentities ?? Enumerable.Empty<string>()).Any()))
                {
                    var currentContext = _contentRenderingContextResolver.Current;
                    if (currentContext != null)
                        currentContext.PreventCache = true;
                }
                    

                var htmlHelper = new HtmlHelper(viewContext, new ViewPage());
                writer.Write(htmlHelper.XhtmlString(xhtmlString));
            }
        }
    }
}
