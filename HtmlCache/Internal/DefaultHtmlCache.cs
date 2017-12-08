using EPiServer.Core;
using EPiServer.Framework.Cache;
using EPiServer.Logging;
using EPiServer.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlCache.Internal
{
    public class DefaultHtmlCache : IHtmlCache
    {
        private readonly IDistributedCache _htmlCache;
        private readonly IRequestCache _requestCache;
        private readonly IPrincipalAccessor _principalAccessor;

        private static ILogger _log = LogManager.GetLogger(typeof(DefaultHtmlCache));

        public const string ContextKey = "Epi:HtmlCacheContext";
        private const string DependencyPrefix = "Ep:h:c:";
        private const string ListingDependencyPrefix = "Ep:h:l:";

        public DefaultHtmlCache(IDistributedCache htmlCache, IRequestCache requestCache, IPrincipalAccessor principalAccessor)
        {
            _htmlCache = htmlCache;
            _requestCache = requestCache;
            _principalAccessor = principalAccessor;
        }

        public string GetOrAdd(string key, Func<IRenderingContext, string> renderingContext)
        {
            var currentContext = _requestCache.Get<RenderingContext>(ContextKey);

            //Do not use cache for authenticated users or if currentContext prevents it
            bool shouldCache = !(_principalAccessor.Principal.Identity.IsAuthenticated || (currentContext?.PreventCache).GetValueOrDefault());
            var cachedResult = shouldCache ? _htmlCache.Get(key) : (string)null;
            if (cachedResult != null)
                return cachedResult;
          
            var newContext = new RenderingContext(currentContext, key)
            {
                PreventCache = !shouldCache
            };
            _requestCache.Set(ContextKey, newContext);

            var htmlResult = renderingContext(newContext);

            //If something should not be cached like a personalized content area item, then we should not cache the content area as whole either
            if (currentContext != null && newContext.PreventCache)
                currentContext.PreventCache = newContext.PreventCache;

            if (!newContext.PreventCache && shouldCache)
            {
                _htmlCache.Set(key, htmlResult);
                if (currentContext != null)
                    _htmlCache.AddDependency($"{DependencyPrefix}{key}", currentContext.Key);

                foreach (var contentLink in newContext.ContentItems)
                    _htmlCache.AddDependency($"{DependencyPrefix}{contentLink.ToReferenceWithoutVersion()}", key);

                foreach (var childListing in newContext.Listings)
                    _htmlCache.AddDependency($"{ListingDependencyPrefix}{childListing.ToReferenceWithoutVersion()}", key);

            }
            _requestCache.Set(ContextKey, currentContext);

            return htmlResult;
        }

        internal void ContentChanged(ContentReference contentLink)
        {
            RemoveKey(contentLink.ToReferenceWithoutVersion().ToString(), DependencyPrefix);
        }

        internal void ChildrenListingChanged(ContentReference contentLink)
        {
            RemoveKey(contentLink.ToReferenceWithoutVersion().ToString(), ListingDependencyPrefix);
        }

        private void RemoveKey(string key, string prefix = null)
        {
            var affectedKeys = new HashSet<string>();
            CollectDependencyKeys(affectedKeys, key, prefix);

            _htmlCache.Remove(affectedKeys);
        }

        private void CollectDependencyKeys(HashSet<string> dependencies, string key, string prefix = null)
        {
            dependencies.Add(key);
            var keyDependencies = _htmlCache.GetDependencies($"{prefix ?? DependencyPrefix}{key}");

            foreach (var dependencyKey in keyDependencies)
                CollectDependencyKeys(dependencies, dependencyKey);
        }
    }
}
