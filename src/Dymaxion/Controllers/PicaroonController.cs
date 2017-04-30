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

        [HttpGet("picaroon/categories")]
        public async Task<IActionResult> GetCategories(string baseUrl = "https://unblockedbay.info")
        {
            var categories = await this.mediator.Send(new GetCategoriesQuery { BaseUrl = baseUrl });

            return Ok(categories);
        }
    }


}