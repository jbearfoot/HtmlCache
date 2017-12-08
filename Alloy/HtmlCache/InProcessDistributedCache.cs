using EPiServer.Framework.Cache;
using HtmlCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Alloy.HtmlCache
{
    public class InProcessDistributedCache : IDistributedCache
    {
        private readonly ISynchronizedObjectInstanceCache _localCache;

        public InProcessDistributedCache(ISynchronizedObjectInstanceCache localCache)
        {
            _localCache = localCache;
        }

        public void AddDependency(string key, string dependencyKey)
        {
            var dependencies = _localCache.Get<IList<string>>(key, ReadStrategy.Immediate);
            if (dependencies == null)
            {
                dependencies = new List<string>();
                _localCache.Insert(key, dependencies, CacheEvictionPolicy.Empty);
            }
            dependencies.Add(dependencyKey);
        }

        public string Get(string key)
        {
            return _localCache.Get<string>(key, ReadStrategy.Immediate);
        }

        public IEnumerable<string> GetDependencies(string key)
        {
            return _localCache.Get<IList<string>>(key, ReadStrategy.Immediate) ?? Enumerable.Empty<string>();
        }

        public void Remove(IEnumerable<string> keys)
        {
            foreach (var key in keys)
                _localCache.Remove(key);
        }

        public void Set(string key, string value)
        {
            _localCache.Insert(key, value, CacheEvictionPolicy.Empty);
        }
    }
}