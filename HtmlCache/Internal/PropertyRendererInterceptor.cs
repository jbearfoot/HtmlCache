using EPiServer.Web.Mvc.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Web.Mvc;
using EPiServer.Core;
using EPiServer.Web.Routing;
using EPiServer.Web;
using Microsoft.Extensions.Internal;
using System.Web.Routing;
using EPiServer.Framework.Web;
using EPiServer.DataAbstraction;
using EPiServer.Web.Mvc;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using EPiServer.Framework.DataAnnotations;
using System.IO;

namespace HtmlCache.Internal
{
    public class PropertyRendererInterceptor : PropertyRenderer
    {
        private readonly PropertyRenderer _defaultRenderer;
        private readonly IContextModeResolver _contextModeResolver;
        private readonly IHtmlCache _htmlCache;
        private readonly TemplateResolver _templateResolver;
        private readonly CachingViewEnginesWrapper _viewResolver;
        public const string CachePrefix = "Prop";

        public PropertyRendererInterceptor(PropertyRenderer defaultRenderer, IContextModeResolver contextModeResolver, IHtmlCache htmlCache, TemplateResolver templateResolver, CachingViewEnginesWrapper viewResolver)
        {
            _defaultRenderer = defaultRenderer;
            _contextModeResolver = contextModeResolver;
            _htmlCache = htmlCache;
            _templateResolver = templateResolver;
            _viewResolver = viewResolver;
        }

        public override MvcHtmlString PropertyFor<TModel, TValue>(HtmlHelper<TModel> html, string viewModelPropertyName, object additionalViewData, object editorSettings, Expression<Func<TModel, TValue>> expression, Func<string, MvcHtmlString> displayForAction)
        {
            Func<MvcHtmlString> defaultRendering = () => _defaultRenderer.PropertyFor(html, viewModelPropertyName, additionalViewData, editorSettings, expression, displayForAction);
            //We do not want cache in edit mode
            if (_contextModeResolver.CurrentMode.EditOrPreview())
                return defaultRendering();

            var additionalValuesDictionary = new RouteValueDictionary(additionalViewData);
            var contentDependecies = GetContentDependecies<TModel>(html, additionalValuesDictionary); 

            //We need to have a cachekey that is unique for the context of this property rendering. We use propertyname, content items, and template to build up the key
            return contentDependecies.Any() ? new MvcHtmlString(_htmlCache.GetOrAdd(CreateCacheKey(contentDependecies, viewModelPropertyName, ResolveTemplateName(expression, html, additionalValuesDictionary)), c =>
            {
                c.AddDependencies(contentDependecies);
                return defaultRendering().ToString();
            })) : 
            defaultRendering();
        }

        //This methods are overloade just to make sure the default registered renderer is called and not this base class
        public override Func<RouteValueDictionary, string, string> CustomSettingsAttributeWriter { get => _defaultRenderer.CustomSettingsAttributeWriter; set => _defaultRenderer.CustomSettingsAttributeWriter = value; }
        public override MvcHtmlString BeginEditSection(HtmlHelper helper, string htmlElement, string propertyKey, string propertyName, object htmlAttributes)
        {
            return _defaultRenderer.BeginEditSection(helper, htmlElement, propertyKey, propertyName, htmlAttributes);
        }
        public override EditContainer CreateEditElement(HtmlHelper helper, string epiPropertyKey, string epiPropertyName, string editElementName, string editElementCssClass, Func<string> renderSettingsAttributeWriter, Func<string> editorSettingsAttributeWriter, TextWriter writer)
        {
            return _defaultRenderer.CreateEditElement(helper, epiPropertyKey, epiPropertyName, editElementName, editElementCssClass, renderSettingsAttributeWriter, editorSettingsAttributeWriter, writer);
        }
        public override MvcHtmlString EditAttributes(HtmlHelper helper, string propertyKey, string propertyName)
        {
            return _defaultRenderer.EditAttributes(helper, propertyKey, propertyName);
        }

        private static string CreateCacheKey(IEnumerable<ContentReference> contentDependencies, params string[] variables)
        {
            var hashcodeCombiner = new HashCodeCombiner();
            hashcodeCombiner.Add(contentDependencies);
            foreach (var variable in variables)
                hashcodeCombiner.Add(variable);

            return $"{CachePrefix}{hashcodeCombiner.CombinedHash}";
        }

        private IEnumerable<ContentReference> GetContentDependecies<TModel>(HtmlHelper<TModel> html, RouteValueDictionary additionalValuesDictionary)
        {
            //Normally propertyfor is dependent on current routed/rendered content instance. However in some case like footer
            //it is another dependency than current content
            var contentDependencies = additionalValuesDictionary[HtmlCacheConstants.Dependencies] as IEnumerable<ContentReference>;
            if (contentDependencies != null)
                return contentDependencies;

            var currentContent = GetCurrentContent<TModel>(html);
            return ContentReference.IsNullOrEmpty(currentContent) ? Enumerable.Empty<ContentReference>() : new[] { currentContent };
        }

        private static ContentReference GetCurrentContent<TModel>(HtmlHelper<TModel> html)
        {
            var content = html.ViewContext.RequestContext.RouteData.Values[RoutingConstants.CurrentContentKey] as IContent;
            if (content != null)
            {
                return content.ContentLink;
            }

            content = html.ViewContext.RequestContext.GetRoutedData<IContent>();
            if (content != null)
            {
                return content.ContentLink;
            }

            return html.ViewContext.RequestContext.GetContentLink();
        }

        private string ResolveTemplateName<TModel, TValue>(Expression<Func<TModel, TValue>> expression, HtmlHelper<TModel> html, RouteValueDictionary additionalValues)
        {
            var metaDataModel = ModelMetadata.FromLambdaExpression(expression, html.ViewData);

            var tag = additionalValues["tag"] as string;
            if (String.IsNullOrEmpty(tag) && metaDataModel != null)
            {
                tag = GetTagFromModelMetadata(metaDataModel);
            }

            if (!String.IsNullOrEmpty(tag) && metaDataModel != null)
            {
                var templateModel = _templateResolver.Resolve(html.ViewContext.HttpContext, metaDataModel.ModelType, metaDataModel.Model, TemplateTypeCategories.MvcPartialView,
                    tag);

                var templateName = GetTemplateName(templateModel, html.ViewContext);
                if (!String.IsNullOrEmpty(templateName))
                {
                    return templateName;
                }
            }

            return ExistDisplayTemplateWithName(html.ViewContext, tag) ? tag : null;
        }

        private string GetTemplateName(TemplateModel templateModel, ControllerContext viewContext)
        {
            if (templateModel == null)
            {
                return null;
            }

            return ExistDisplayTemplateWithName(viewContext, templateModel.Name) ? templateModel.Name : null;
        }

        private bool ExistDisplayTemplateWithName(ControllerContext viewContext, string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                return false;
            }

            var result = _viewResolver.FindPartialView(viewContext, "DisplayTemplates/" + templateName);
            return result != null && result.View != null;
        }

        private static string GetTagFromModelMetadata(ModelMetadata metaData)
        {
            if (metaData == null || metaData.ContainerType == null)
            {
                return null;
            }

            var prop = metaData.ContainerType.GetProperty(metaData.PropertyName);
            if (prop != null)
            {
                var hintAttributes = prop.GetCustomAttributes(true).OfType<UIHintAttribute>();

                var specificTemplateLayerAttribute = hintAttributes.FirstOrDefault(a => string.Equals(a.PresentationLayer, PresentationLayer.Website, StringComparison.OrdinalIgnoreCase));

                if (specificTemplateLayerAttribute != null)
                {
                    //If we find a UI hint attribute with presentation layer == "website" this take precedence.
                    return specificTemplateLayerAttribute.UIHint;
                }

                var genericHintAttribute = hintAttributes.FirstOrDefault(a => String.IsNullOrEmpty(a.PresentationLayer));

                if (genericHintAttribute != null)
                {
                    //If we don't have a specific "template" hint we try to find a hint with an unspecified presentation layer.
                    return genericHintAttribute.UIHint;
                }
            }

            return null;
        }
    }
}
