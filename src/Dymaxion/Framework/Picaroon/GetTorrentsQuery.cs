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
    public enum Direction : int
    {
        Ascending,
        Descending
    }

    public enum OrderBy : int
    {
        Uploaded,
        Size,
        Uploader,
        Seeders,
        Leechers,
        Name
    }

    public class GetTorrentsQuery : IRequest<GetTorrentsResponse>
    {
        public string Keywords { get; set; }
        public string CategoryID { get; set; }
        public string BaseUrl { get; set; }
        public int Page { get; set; } = 0;
        public Direction Direction { get; set; } = Direction.Descending;
        public OrderBy OrderBy { get; set; } = OrderBy.Uploaded;
    }

    public class GetTorrentsQueryHandler : IAsyncRequestHandler<GetTorrentsQuery, GetTorrentsResponse>
    {
        private static readonly Regex categoryIDRegex = new Regex(@"/browse/(?<categoryID>[\d]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private static readonly Regex torrentIDRegex = new Regex(@"/torrent/(?<torrentID>[\d]+)/.*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private readonly IMediator mediator;

        public GetTorrentsQueryHandler(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<GetTorrentsResponse> Handle(GetTorrentsQuery message)
        {
            using (var restClient = new RestClient(message.BaseUrl))
            {
                string resource;

                if (string.IsNullOrEmpty(message.Keywords))
                {
                    resource = "browse/{categoryID}/{page}/{sortID}";
                }
                else
                {
                    resource = "search/{keywords}/{page}/{sortID}/{categoryID}";
                }

                var sortID = GetSortID(message.OrderBy, message.Direction);

                var request = new RestRequest(HttpMethod.Get, resource);
                request.AddParameter("keywords", message.Keywords, ParameterType.UrlSegment);
                request.AddParameter("categoryID", message.CategoryID, ParameterType.UrlSegment);
                request.AddParameter("page", message.Page, ParameterType.UrlSegment);
                request.AddParameter("sortID", sortID, ParameterType.UrlSegment);

                var response = await restClient.Execute<string>(request);

                var html = await response.GetContent();

                return this.Parse(html);
            }
        }

        private int GetSortID(OrderBy orderBy, Direction direction)
        {
            if (orderBy == OrderBy.Leechers && direction == Direction.Ascending)
            {
                return 8;
            }
            else if (orderBy == OrderBy.Leechers && direction == Direction.Descending)
            {
                return 7;
            }
            else if (orderBy == OrderBy.Seeders && direction == Direction.Ascending)
            {
                return 10;
            }
            else if (orderBy == OrderBy.Seeders && direction == Direction.Descending)
            {
                return 9;
            }
            else if (orderBy == OrderBy.Size && direction == Direction.Ascending)
            {
                return 6;
            }
            else if (orderBy == OrderBy.Size && direction == Direction.Descending)
            {
                return 5;
            }
            else if (orderBy == OrderBy.Uploaded && direction == Direction.Ascending)
            {
                return 4;
            }
            else if (orderBy == OrderBy.Uploaded && direction == Direction.Descending)
            {
                return 3;
            }
            else if (orderBy == OrderBy.Uploader && direction == Direction.Ascending)
            {
                return 11;
            }
            else if (orderBy == OrderBy.Uploader && direction == Direction.Descending)
            {
                return 12;
            }
            else if (orderBy == OrderBy.Name && direction == Direction.Ascending)
            {
                return 1;
            }
            else if (orderBy == OrderBy.Name && direction == Direction.Descending)
            {
                return 2;
            }
            return 0;
        }

        private GetTorrentsResponse Parse(string html)
        {
            var document = new HtmlAgilityPack.HtmlDocument()
            {
                OptionFixNestedTags = true,
                OptionAutoCloseOnEnd = true
            };

            document.LoadHtml(html);

            var response = new GetTorrentsResponse();

            var nodes = document.QuerySelectorAll("table#searchResult tr");

            foreach (var node in nodes)
            {
                var torrent = new Torrent();

                torrent.CategoryID = GetValue(categoryIDRegex, "categoryID", node.QuerySelector("td.vertTh center a:nth-child(1)")?.Attributes["href"]?.Value);
                torrent.SubcategoryID = GetValue(categoryIDRegex, "categoryID", node.QuerySelector("td.vertTh center a:nth-child(3)")?.Attributes["href"]?.Value);
                torrent.Category = node.QuerySelector("td.vertTh center a:nth-child(1)")?.InnerText;
                torrent.Subcategory = node.QuerySelector("td.vertTh center a:nth-child(3)")?.InnerText;
                torrent.Name = node.QuerySelector("a.detLink")?.InnerText;
                torrent.TorrentID = GetValue(torrentIDRegex, "torrentID", node.QuerySelector("a.detLink")?.Attributes["href"]?.Value);
                torrent.Seeders = int.Parse(node.QuerySelector("td:nth-child(3)")?.InnerText ?? "0");
                torrent.Leechers = int.Parse(node.QuerySelector("td:nth-child(4)")?.InnerText ?? "0");

                var description = node.QuerySelector("font.detDesc")?.InnerText;

                if (!string.IsNullOrEmpty(description))
                {
                    var items = description.Split(',').Select(x => x.Trim());

                    torrent.Uploaded = items.Where(x => x.StartsWith("Uploaded ")).Select(x => x.Replace("Uploaded ", string.Empty).Trim()).FirstOrDefault();
                    torrent.Size = items.Where(x => x.StartsWith("Size ")).Select(x => x.Replace("Size ", string.Empty).Trim()).FirstOrDefault();
                    torrent.Uploader = items.Where(x => x.StartsWith("ULed by ")).Select(x => x.Replace("ULed by ", string.Empty).Trim()).FirstOrDefault();
                }

                if (!string.IsNullOrEmpty(torrent.TorrentID))
                {
                    response.Torrents.Add(torrent);
                }
            }

            return response;
        }

        private string GetValue(Regex regex, string name, string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var match = regex.Match(text);

                if (match.Success)
                {
                    return match.Groups[name]?.Value;
                }
            }

            return null;
        }
    }

    public class Torrent
    {
        public string CategoryID { get; set; }
        public string SubcategoryID { get; set; }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string TorrentID { get; set; }
        public string Name { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }
        public string Uploaded { get; set; }
        public string Size { get; set; }
        public string Uploader { get; set; }
    }

    public class GetTorrentsResponse
    {
        public List<Torrent> Torrents { get; set; } = new List<Torrent>();
    }
}