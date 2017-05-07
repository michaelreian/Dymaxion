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
using Serilog;

namespace DataReaver.Framework.Picaroon
{

    public class GetProxyQuery : IRequest<Uri>
    {
    }

    public class GetProxyQueryHandler : IAsyncRequestHandler<GetProxyQuery, Uri>
    {
        private readonly IMediator mediator;
        private readonly ILogger logger;

        public GetProxyQueryHandler(IMediator mediator, ILogger logger)
        {
            this.mediator = mediator;
            this.logger = logger;
        }

        public async Task<Uri> Handle(GetProxyQuery message)
        {
            var proxies = await this.mediator.Send(new GetProxiesQuery());

            foreach(var proxy in proxies.OrderBy(p => p.Speed))
            {
                var builder = new UriBuilder(proxy.Secure ? "https" : "http", proxy.Domain);

                var uri = builder.Uri;

                if (await this.Check(uri))
                {
                    return uri;
                }
            }

            return null;
        }

        private async Task<bool> Check(Uri uri)
        {
            try
            {
                using (var restClient = new RestClient(uri))
                {
                    var response = await restClient.Get<string>("");

                    response.EnsureSuccessStatusCode();
                }

                return true;
            }
            catch (Exception exception)
            {
                this.logger.Warning(exception, "There was an error checking {Uri}", uri);
            }
            return false;
        }


    }

    public class PicaroonProxy
    {
        public string Domain { get; set; }
        public decimal Speed { get; set; }
        public bool Secure { get; set; }
        public string Country { get; set; }
        public bool Probed { get; set; }
    }


    public class GetProxiesQuery : IRequest<List<PicaroonProxy>>
    {
    }

    public class GetProxiesQueryHandler : IAsyncRequestHandler<GetProxiesQuery, List<PicaroonProxy>>
    {
        private static readonly string THE_PIRATE_BAY_PROXY_LIST_URL = "https://thepiratebay-proxylist.org/api/v1/proxies";

        private readonly IMediator mediator;

        public GetProxiesQueryHandler(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<List<PicaroonProxy>> Handle(GetProxiesQuery message)
        {
            using (var restClient = new RestClient())
            {
                var response = await restClient.Get<Result>(THE_PIRATE_BAY_PROXY_LIST_URL);

                response.EnsureSuccessStatusCode();

                return await response.GetData(x => x.Proxies);
            }
        }

        private class Result
        {
            public List<PicaroonProxy> Proxies { get; set; }
        }
    }

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
            using (var restClient = new RestClient(message.BaseUrl))
            {
                var response = await restClient.Get<string>("browse");

                var html = await response.GetContent();

                return this.Parse(html);
            }
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
                    var subcategory = new Category();

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
        public List<Category> Subcategories { get; set; } = new List<Category>();
    }
}