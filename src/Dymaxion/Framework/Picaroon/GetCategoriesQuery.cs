using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSwag.Annotations;
using MediatR;
using System.Net.Http;
using HtmlAgilityPack.CssSelectors.NetCore;
using System.Text.RegularExpressions;

namespace DataReaver.Framework.Picaroon
{
    public class GetCategoriesQuery : IRequest<List<Category>>
    {
        public string BaseUrl { get; set; }
    }

    public class GetCategoriesQueryHandler : IAsyncRequestHandler<GetCategoriesQuery, List<Category>>
    {
        private static readonly Regex categoryIDRegex = new Regex(@"/browse/(?<categoryID>[\d]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private readonly IMediator mediator;

        public GetCategoriesQueryHandler(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<List<Category>> Handle(GetCategoriesQuery message)
        {
            var builder = new UriBuilder(message.BaseUrl);

            builder.Path = "browse";

            var html = await this.mediator.Send(new GetHtmlQuery { RequestUri = builder.ToString() });

            return this.Parse(html);
        }

        private List<Category> Parse(string html)
        {
            var document = new HtmlAgilityPack.HtmlDocument()
            {
                OptionFixNestedTags = true,
                OptionAutoCloseOnEnd = true
            };

            document.LoadHtml(html);

            var categories = new List<Category>();

            var categoryNodes = document.QuerySelectorAll("td.categoriesContainer dl dt a");

            var subcategoryNodes = document.QuerySelectorAll("select#category optgroup");

            foreach (var categoryNode in categoryNodes)
            {
                var category = new Category();

                category.ID = ParseCategoryID(categoryNode.GetAttributeValue("href", null));
                category.Name = categoryNode.InnerText;

                foreach(var subcategoryNode in subcategoryNodes.Where(x => x.GetAttributeValue("label", null) == category.Name).SelectMany(x => x.QuerySelectorAll("option")))
                {
                    var subcategory = new Subcategory();

                    subcategory.ID = subcategoryNode.GetAttributeValue("value", null);
                    subcategory.Name = subcategoryNode.NextSibling.InnerText;

                    category.Subcategories.Add(subcategory);
                }

                categories.Add(category);
            }

            return categories;
        }

        private string ParseCategoryID(string id)
        {
            if(!string.IsNullOrEmpty(id))
            {
                var match = categoryIDRegex.Match(id);

                if (match.Success)
                {
                    return match.Groups["categoryID"]?.Value;
                }
            }

            return null;
        }
    }

    public class Category
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public List<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
    }

    public class Subcategory
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }
}