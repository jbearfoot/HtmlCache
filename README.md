# A HTML cache for EPiServer CMS based on Russian doll pattern

## IHtmlCache
I have created an interface IHtmlCache that can be used to get/add html to the cache. The interface is registered in the IOC container. The interface has a single method like:

*string GetOrAdd(string key, Func<IRenderingContext, string> renderingCallback);*

The way to use the method is to first create a cache key that should be unique for the context (colud be built from e.g. current content, current property and current template). Then a call is maid against the method *GetOrAdd*. If the item is already cached you get the html result back otherwise the renderingCallback Func is called where you specify how to render the html. Then the result is cached and returned. The argument *IRenderingContext* that gets passed to the callback makes it possible to for example add content dependencies. The current *IRenderingContext* can also be retrieved from IOC container using *IRenderingContextResolver*. This can be useful if you have for example a listing block that is dependent on some listing, then you can add a dependecy to that listing in for example the block controller (see *PageListBlockController* in my Alloy project).

## PropertyFor and Content rendering
To inject the html cache in the CMS rendering I choosed to intercept *PropertyRenderer*, *ContentAreaRenderer* and *IContentRenderer*. The interceptors basically checks if the html output for the item to be rendered is already cached. If so the html is returned else the default renderer is called and the output is captured and cached. During the rendering it also creates dependencies to the content item(s) that the output is dependent on. That is so the right html elements gets evicted for example when a content item is published.

## Personalization
This caching technique is somewhat related to output cache, but in that case is the whole html for a page cached as whole. One problem is that is hard to know which content item that affected the rendering and hence is the whole output cache evicted (for all pages) when some content is published. Also output cache is hard to use with personalization since it is all or nothing that gets cached. With russian doll caching it was possible to handle personalization much nicer. Say for example that a page contains a content area or a xhtmlstring that is personalized. Then we can avoid to cache that specific property but still cache the html for other items on that page such as menus, listings and other contentareas or xhtmlstrings.

## Alloy modifications
I have uploaded a somewhat modified Alloy site together with the code for the html cache to my git hub repository. I have added a Block type NowBlock which can be used to verify that personalization of ContentArea and XhtmlString works as expected. I have also added an action filter that writes out the rendering time for request in the bottom left of the page. There are also a noop and an in-process implemenation of *IDistributedCache* that can be registered by selecting which IDistributedCache implementation that is registered in HtmlCache/DistributedCacheConfigurationModule.cs. That can be used to compare the rendering time for inprocess-cache, redis cache and no html cache (it still uses the content cache in this case).

### Menu
I have changed the Menu in Helpers/HtmlHelpers.cs so it uses the html cache. You can see in the code that it also caches at several layers, first the menu as whole but also individual menu items. Meaning if one item would be evicted then that item and the menu as whole needs to get re-rendered but the html output from all other items remains and can be reused to render the menu as whole.

### Footer
On Alloy all pages uses the same footer that has some page listings. However those page listing is not dependent on the current rendered page, instead those are defined on start page. Therefore has the PropertyFor calls in Footer.cshtml been modified to the pass in which content it is dependent on(the default behaviour is current rendered content).

## Thougths
In my implementation I went "All-In" meaning it caches a whole lot for example the output all properties. It would be possible to adjust it to just cache some specific property types (e.g. ContentArea and XhtmlString) or to avoid caching property output at all. A suggestion could be to profile the application to see what gives most benefit (menus and listings are good candidates) and start with that. Especially if you cache in-process you might want to limit what to cache. However I strongly suggest a distributed cache, that also addresses the cold start scenario. 

## Disclamier
This is nothing offically supported by EPiServer, you are free to use it as you like at your own risk.