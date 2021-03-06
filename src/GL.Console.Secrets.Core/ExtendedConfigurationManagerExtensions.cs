﻿using GL.Console.Secrets.Core.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GL.Console.Secrets.Extensions
{
    public static class ExtendedConfigurationManagerExtensions
    {
        public static void AddSecretsFromJson(this ExtendedConfigurationManager manager, string secretsPath)
        {
            if (!File.Exists(secretsPath))
                throw new FileNotFoundException("Unable to find secrets file", secretsPath);

            using (var streamReader = new StreamReader(secretsPath))
            {
                InternalHelper.AddRange(manager, streamReader.ReadToEnd());
            }
        }

        public static void AddSecretsFromEndpoint(this ExtendedConfigurationManager manager, string endpointUrl)
        {
            if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out Uri uriResult) || (uriResult?.Scheme != Uri.UriSchemeHttp && uriResult?.Scheme != Uri.UriSchemeHttps))
                throw new UriFormatException("Invalid endpoint URL provided or non HTTPS URL");

            using (var client = new HttpClient())
            {
                var httpJsonResponse = client.GetStringAsync(new Uri(endpointUrl)).GetAwaiter().GetResult();
                InternalHelper.AddRange(manager, httpJsonResponse);
            }
        }

        public static void AddSecretFromProtectedEndpointAsync(this ExtendedConfigurationManager manager, string endpointUrl, Func<Task<string>> tokenAction, Func<HttpResponseMessage, Task<Dictionary<string, string>>> deserializationHandler)
        {
            if (tokenAction == null)
                throw new ArgumentNullException(nameof(tokenAction));

            if (deserializationHandler == null)
                throw new ArgumentNullException(nameof(deserializationHandler));

            if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out Uri uriResult) || (uriResult?.Scheme != Uri.UriSchemeHttp && uriResult?.Scheme != Uri.UriSchemeHttps))
                throw new UriFormatException("Invalid endpoint URL provided or non HTTPS URL");

            using (var client = new HttpClient())
            {
                var header = tokenAction.Invoke().GetAwaiter().GetResult();
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", header);

                var httpResponse = client.GetAsync(new Uri(endpointUrl)).GetAwaiter().GetResult();
                httpResponse.EnsureSuccessStatusCode();

                var settings = deserializationHandler.Invoke(httpResponse).GetAwaiter().GetResult();
                InternalHelper.AddRange(manager, settings);
            }
        }
    }
}
