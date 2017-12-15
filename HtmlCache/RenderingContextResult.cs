using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HtmlCache
{
    public class RenderingContextResult
    {
        public IRenderingContext StartedContext { get; set; }
        public string CachedResult { get; set; }
    }
}
