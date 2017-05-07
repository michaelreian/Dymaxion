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
using DataReaver.Framework.Picaroon;

namespace DataReaver.Controllers
{
    public class PicaroonController : Controller
    {
        private readonly IMediator mediator;

        public PicaroonController(IMediator mediator)
        {
            this.mediator = mediator;
        }

        [HttpGet("")]
        [SwaggerIgnore]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("api/v1/torrents/browse")]
        public async Task<IActionResult> Browse(string baseUrl = "https://unblockedbay.info", string categoryID = null)
        {
            var results = await this.mediator.Send(new GetTorrentsQuery { BaseUrl = baseUrl, CategoryID = categoryID });

            return Ok(results);
        }

        [HttpGet("api/v1/torrents/categories")]
        public async Task<IActionResult> GetCategories(string baseUrl = "https://unblockedbay.info")
        {
            var categories = await this.mediator.Send(new GetCategoriesQuery { BaseUrl = baseUrl });

            return Ok(categories);
        }

        [HttpGet("api/v1/torrents/proxies")]
        public async Task<IActionResult> GetProxies()
        {
            var proxies = await this.mediator.Send(new GetProxiesQuery());

            return Ok(proxies);
        }

        [HttpGet("api/v1/torrents/proxies/valid")]
        public async Task<IActionResult> GetValidProxy()
        {
            var uri = await this.mediator.Send(new GetProxyQuery());

            return Ok(uri);
        }
    }


}