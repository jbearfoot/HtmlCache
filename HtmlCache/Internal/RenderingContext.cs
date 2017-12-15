using EPiServer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlCache.Internal
{
    public class RenderingContext : IRenderingContext
    {
        private readonly RenderingContext _parentContext;
        private readonly string _key;
        private List<ContentReference> _contentItems = new List<ContentReference>();
        private IList<ContentReference> _childrenListings = new List<ContentReference>();


        public RenderingContext(RenderingContext parentContext, string key)
        {
            _parentContext = parentContext;
            _key = key;
        }

        public string Key => _key;
        public IRenderingContext ParentContext => _parentContext;

        public IEnumerable<ContentReference> ContentItems => _contentItems;
        public IEnumerable<ContentReference> Listings => _childrenListings;

        public bool DependentOnAllContent { get; set; }
        public bool PreventCache { get; set; } = false;

        public void AddChildrenListingDependency(ContentReference contentLink)
        {
            _childrenListings.Add(contentLink);
        }

        public void AddDependencies(IEnumerable<ContentReference> contentLinks)
        {
            _contentItems.AddRange(contentLinks);
        }
    }
}
