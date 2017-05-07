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
using Newtonsoft.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace DataReaver.Framework
{

    public interface IRestClient : IDisposable
    {
        Task<RestResponse<T>> Execute<T>(RestRequest request);
        Task<RestResponse<T>> Get<T>(string resource);
    }

    public class RestClient : IRestClient
    {
        private HttpClient httpClient;

        public List<Parameter> Parameters { get; } = new List<Parameter>();

        public RestClient()
        {
            this.httpClient = new HttpClient();
        }

        public RestClient(Uri baseAddress)
        {
            this.httpClient = new HttpClient
            {
                BaseAddress = baseAddress
            };
        }

        public RestClient(string baseAddress) : this(new Uri(baseAddress))
        {
        }

        public Uri BaseAddress
        {
            get
            {
                return this.httpClient.BaseAddress;
            }
            set
            {
                this.httpClient.BaseAddress = value;
            }
        }

        public TimeSpan Timeout
        {
            get
            {
                return this.httpClient.Timeout;
            }
            set
            {
                this.httpClient.Timeout = value;
            }
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        public async Task<RestResponse<T>> Execute<T>(RestRequest request)
        {
            var requestUri = this.BuildUri(request);

            var httpRequestMessage = new HttpRequestMessage(request.Method, requestUri);

            var httpResponseMessage = await this.httpClient.SendAsync(httpRequestMessage);

            return new RestResponse<T>(httpResponseMessage);
        }


        public async Task<RestResponse<T>> Get<T>(string resource)
        {
            return await this.Execute<T>(new RestRequest(HttpMethod.Get, resource));
        }

        private List<Parameter> GetParameters(RestRequest request)
        {
            var parameters = new List<Parameter>(this.Parameters);
            foreach (var parameter in request.Parameters)
            {
                var existingParameter = parameters.SingleOrDefault(p => p.Name.Equals(parameter.Name) && p.Type == parameter.Type);
                if (existingParameter != null)
                {
                    parameters.Remove(existingParameter);
                }
                parameters.Add(parameter);
            }
            return parameters;
        }

        public Uri BuildUri(RestRequest request)
        {
            Uri uri;

            if(this.BaseAddress == null)
            {
                uri = new Uri(request.Resource);
            }
            else if(!string.IsNullOrEmpty(request.Resource))
            {
                uri = new Uri(this.BaseAddress, request.Resource);
            }
            else
            {
                uri = this.BaseAddress;
            }

            var builder = new UriBuilder(uri);

            builder.Path = this.GetPath(request, builder.Path);

            builder.Query = this.GetQuery(request, builder.Query);

            return builder.Uri;
        }

        private string GetQuery(RestRequest request, string originalQuery)
        {
            if (string.IsNullOrEmpty(originalQuery))
            {
                return originalQuery;
            }

            var dictionary = QueryHelpers.ParseQuery(originalQuery);

            foreach (var parameter in this.GetParameters(request).Where(p => p.Type == ParameterType.QueryString || (request.Method == HttpMethod.Get && p.Type == ParameterType.GetOrPost)))
            {
                if(dictionary.ContainsKey(parameter.Name))
                {
                    dictionary.Remove(parameter.Name);
                }

                dictionary.Add(parameter.Name, parameter.Value?.ToString());
            }

            return string.Join("&", dictionary.Select(x => $"{Uri.EscapeUriString(x.Key)}={Uri.EscapeUriString(x.Value.ToString())}"));
        }

        private string GetPath(RestRequest request, string originalPath)
        {
            if (string.IsNullOrEmpty(originalPath))
            {
                return originalPath;
            }

            var path = Uri.UnescapeDataString(originalPath);

            foreach (var parameter in this.GetParameters(request).Where(p => p.Type == ParameterType.UrlSegment))
            {
                path = path.Replace($"{{{parameter.Name}}}", Uri.EscapeUriString(parameter.Value?.ToString() ?? string.Empty));
            }

            return path;
        }

    }

    public class RestRequest
    {
        public HttpMethod Method { get; set; }
        public string Resource { get; set; }

        public List<Parameter> Parameters { get; } = new List<Parameter>();


        public RestRequest(HttpMethod method, string resource)
        {
            this.Method = method;
            this.Resource = resource;
        }

        public RestRequest AddParameter(Parameter parameter)
        {
            this.Parameters.Add(parameter);

            return this;
        }

        public RestRequest AddParameter(string name, object value, ParameterType type)
        {
            return this.AddParameter(new Parameter
            {
                Name = name,
                Value = value,
                Type = type
            });
        }
    }

    public class RestResponse<T>
    {
        private readonly HttpResponseMessage httpResponseMessage;

        public RestResponse(HttpResponseMessage httpResponseMessage)
        {
            this.httpResponseMessage = httpResponseMessage;
        }

        public void EnsureSuccessStatusCode()
        {
            this.httpResponseMessage.EnsureSuccessStatusCode();
        }

        public async Task<T> GetData()
        {
            return await this.GetData<T>(x => x);
        }

        public async Task<TOutput> GetData<TOutput>(Func<T, TOutput> getDataFunction)
        {
            var content = await this.GetContent();

            var data = JsonConvert.DeserializeObject<T>(content);

            return getDataFunction(data);
        }

        public async Task<string> GetContent()
        {
            return await this.httpResponseMessage.Content.ReadAsStringAsync();
        }
    }

    public enum ParameterType
    {
        Cookie,
        GetOrPost,
        UrlSegment,
        HttpHeader,
        RequestBody,
        QueryString
    }

    public class Parameter
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public ParameterType Type { get; set; }
        public string ContentType { get; set; }
    }
}