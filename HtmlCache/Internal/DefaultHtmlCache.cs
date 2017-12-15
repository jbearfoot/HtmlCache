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
            var contextResult = BeginContext(key);
            if (contextResult.CachedResult != null)
                return contextResult.CachedResult;

            var htmlResult = renderingContext(contextResult.StartedContext);

            CompleteContext(contextResult.StartedContext, htmlResult);

            return htmlResult;
        }

        public RenderingContextResult BeginContext(string key)
        {
            bool authenticated = _principalAccessor.Principal.Identity.IsAuthenticated;
            var cachedResult = (authenticated) ? (string)null : _htmlCache.Get(key);
            if (cachedResult != null)
                return new RenderingContextResult { CachedResult = cachedResult };

            var parentContext = _requestCache.Get<RenderingContext>(ContextKey);
            var currentContext = new RenderingContext(parentContext, key);
            _requestCache.Set(ContextKey, currentContext);

            return new RenderingContextResult { StartedContext = currentContext };
        }

        public void CompleteContext(IRenderingContext currentContext, string htmlResult)
        {
            var parentContext = currentContext.ParentContext;
            //If something should not be cached like a personalized content area item, then we should not cache the content area as whole either
            if (parentContext != null && currentContext.PreventCache)
                parentContext.PreventCache = currentContext.PreventCache;

            if (!currentContext.PreventCache && !_principalAccessor.Principal.Identity.IsAuthenticated)
            {
                _htmlCache.Set(currentContext.Key, htmlResult);
                if (parentContext != null)
                    _htmlCache.AddDependency($"{DependencyPrefix}{currentContext.Key}", parentContext.Key);

                foreach (var contentLink in currentContext.ContentItems)
                    _htmlCache.AddDependency($"{DependencyPrefix}{contentLink.ToReferenceWithoutVersion()}", currentContext.Key);

                foreach (var childListing in currentContext.Listings)
                    _htmlCache.AddDependency($"{ListingDependencyPrefix}{childListing.ToReferenceWithoutVersion()}", currentContext.Key);

            }
            _requestCache.Set(ContextKey, parentContext);
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
