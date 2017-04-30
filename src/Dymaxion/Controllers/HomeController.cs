using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSwag.Annotations;

namespace DataReaver.Controllers
{
    public class HomeController : Controller
    {
        public HomeController()
        {
        }

        [HttpGet("")]
        [SwaggerIgnore]
        public IActionResult Index()
        {
            return View();
        }
    }
}