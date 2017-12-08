using HtmlCache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Alloy.HtmlCache
{
    public class NoDistributedHtmlCache : IDistributedCache
    {
        public void AddDependency(string key, string dependencyKey)
        {
        }

        public string Get(string key)
        {
            return null;
        }

        public IEnumerable<string> GetDependencies(string key)
        {
            return Enumerable.Empty<string>();
        }

        public void Remove(IEnumerable<string> key)
        {
        }

        public void Set(string key, string value)
        {
        }
    }
}