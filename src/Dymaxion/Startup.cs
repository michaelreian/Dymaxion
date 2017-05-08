using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using NJsonSchema;
using NSwag.AspNetCore;
using Serilog;
using MediatR;
using System.Reflection;
using Serilog.Events;
using Dymaxion.Framework;
using Microsoft.AspNetCore.SpaServices.Webpack;

namespace Dymaxion
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment hostingEnvironment)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(hostingEnvironment.ContentRootPath)
                .AddJsonFile("Settings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"Settings.{hostingEnvironment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddOptions();

            services.AddMemoryCache();

            services.AddMediatR(typeof(Startup).GetTypeInfo().Assembly);

            var builder = new ContainerBuilder();

            builder.Populate(services);

            builder.RegisterModule<DymaxionModule>();

            var container = builder.Build();

            return new AutofacServiceProvider(container);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment hostingEnvironment, ILoggerFactory loggerFactory)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            loggerFactory.AddSerilog();

            if (hostingEnvironment.IsDevelopment())
            {
                loggerFactory.AddDebug();
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
                app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true
                });
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseSwaggerUi(typeof(Startup).GetTypeInfo().Assembly, new SwaggerUiOwinSettings
            {
                DefaultPropertyNameHandling = PropertyNameHandling.CamelCase,
                DefaultEnumHandling = EnumHandling.String,
                Version = string.Concat("v", Environment.GetEnvironmentVariable("APP_VERSION") ?? "Next"),
                Title = "Dymaxion",
                IsAspNetCore = true,
                ValidateSpecification = true,
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}