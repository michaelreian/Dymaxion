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

namespace DataReaver.Framework
{
    public class GetHtmlQuery : IRequest<string>
    {
        public string RequestUri { get; set; }
    }

    public class GetHtmlQueryHandler : IAsyncRequestHandler<GetHtmlQuery, string>
    {
        public async Task<string> Handle(GetHtmlQuery message)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(message.RequestUri);

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}