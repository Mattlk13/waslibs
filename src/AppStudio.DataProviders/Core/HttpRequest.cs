﻿using System;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace AppStudio.DataProviders.Core
{
    internal static class HttpRequest
    {
        internal static async Task<HttpRequestResult<TSchema>> ExecuteGetAsync<TSchema>(Uri uri, IParser<TSchema> parser) where TSchema : SchemaBase
        {
            var settings = new HttpRequestSettings()
            {
                RequestedUri = uri
            };

            HttpRequestResult httpResult = await DownloadAsync(settings);
            HttpRequestResult<TSchema> result;
            result = new HttpRequestResult<TSchema>(httpResult);
            if (httpResult.Success)
            {
                var items = parser.Parse(httpResult.Result);
                if (items == null)
                {
                    items = new TSchema[0];
                }
                result.Items = items;
            }

            return result;
        }

        internal static async Task<HttpRequestResult> DownloadAsync(HttpRequestSettings settings)
        {
            var result = new HttpRequestResult();
            HttpResponseMessage response = await GetResponseMessage(settings);
            result.StatusCode = response.StatusCode;
            FixInvalidCharset(response);            
            var content = await response.Content.ReadAsStringAsync();
            result.Result = content;
            return result;
        }

        internal static async Task<HttpRequestResult> DownloadRssAsync(HttpRequestSettings settings)
        {
            var result = new HttpRequestResult();
            HttpResponseMessage response = await GetResponseMessage(settings);
            result.StatusCode = response.StatusCode;
            FixInvalidCharset(response);
            await SetEncoding(response);            
            var content = await response.Content.ReadAsStringAsync();
            result.Result = content;
            return result;
        }

        private static async Task<HttpResponseMessage> GetResponseMessage(HttpRequestSettings settings)
        {  
            var filter = new HttpBaseProtocolFilter();
            filter.CacheControl.ReadBehavior = HttpCacheReadBehavior.MostRecent;

            var httpClient = new HttpClient(filter);

            AddRequestHeaders(httpClient, settings);

            HttpResponseMessage response = await httpClient.GetAsync(settings.RequestedUri);
            return response;
        }

        private static void AddRequestHeaders(HttpClient httpClient, HttpRequestSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.UserAgent))
            {
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(settings.UserAgent);
            }

            if (settings.Headers != null)
            {
                foreach (var customHeaderName in settings.Headers.AllKeys)
                {
                    if (!String.IsNullOrEmpty(settings.Headers[customHeaderName]))
                    {
                        httpClient.DefaultRequestHeaders.Add(customHeaderName, settings.Headers[customHeaderName]);
                    }
                }
            }
        }

        private static void FixInvalidCharset(HttpResponseMessage response)
        {
            if (response?.Content?.Headers?.ContentType?.CharSet != null)
            {
                // Fix invalid charset returned by some web sites.
                string charset = response.Content.Headers.ContentType.CharSet;
                if (charset.Contains("\""))
                {
                    response.Content.Headers.ContentType.CharSet = charset.Replace("\"", string.Empty);
                }
              
            }
        }

        private async static Task SetEncoding(HttpResponseMessage response)
        {
            if (response?.Content?.Headers?.ContentType?.CharSet != null)
            {
                if (response.Content.Headers.ContentType.MediaType?.ToLower().Contains("xml") == true)
                {
                    string charset = response.Content.Headers.ContentType.CharSet;
                    if (string.IsNullOrEmpty(charset))
                    {
                        var content = await response.Content.ReadAsStringAsync();                     
                        var encoding = "UTF-8";
                        try
                        {
                            var doc = XDocument.Parse(content);
                            if (!string.IsNullOrEmpty(doc?.Declaration?.Encoding))
                            {
                                encoding = doc.Declaration.Encoding;
                            }
                            response.Content.Headers.ContentType.CharSet = encoding;
                        }
                        catch (XmlException)
                        {
                            response.Content.Headers.ContentType.CharSet = charset;
                        }
                       
                    }
                }
            }
        }
    }
}
