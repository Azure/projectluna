﻿using Luna.Common.Utils;
using Luna.Publish.Public.Client;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Luna.Publish.Clients
{
    public class HttpRequestParser : IHttpRequestParser
    {
        /// <summary>
        /// Parse and validate a Luna application from request body
        /// </summary>
        /// <param name="requestBody">The http request body</param>
        /// <returns>A LunaApplication instance</returns>
        public async Task<LunaApplicationProp> ParseAndValidateLunaApplicationAsync(string requestBody)
        {
            var app = DeserializeRequestBodyAsync<LunaApplicationProp>(requestBody);
            //TODO: validation
            return app;
        }

        /// <summary>
        /// Parse and validate a Luna API from request body
        /// </summary>
        /// <param name="requestBody">The http request body</param>
        /// <returns>A BaseLunaAPIProp instance</returns>
        public async Task<BaseLunaAPIProp> ParseAndValidateLunaAPIAsync(string requestBody)
        {
            var api = DeserializeRequestBodyAsync<BaseLunaAPIProp>(requestBody);

            LunaAPIType apiType;
            if (!Enum.TryParse<LunaAPIType>(api.Type, out apiType))
            {
                throw new LunaBadRequestUserException(
                    string.Format(ErrorMessages.API_TYPE_NOT_SUPPORTED, api.Type),
                    UserErrorCode.InvalidParameter);
            }
            switch (apiType)
            {
                case LunaAPIType.Realtime:
                    api = DeserializeRequestBodyAsync<RealtimeEndpointLunaAPIProp>(requestBody);
                    break;
                case LunaAPIType.Pipeline:
                    api = DeserializeRequestBodyAsync<PipelineEndpointLunaAPIProp>(requestBody);
                    break;
                case LunaAPIType.MLProject:
                    api = DeserializeRequestBodyAsync<MLProjectLunaAPIProp>(requestBody);
                    break;
                default:
                    throw new LunaBadRequestUserException(
                        string.Format(ErrorMessages.API_TYPE_NOT_SUPPORTED, api.Type),
                        UserErrorCode.InvalidParameter);
            }
            //TODO: validation
            return api;
        }

        /// <summary>
        /// Parse and validate a Luna API version request body
        /// </summary>
        /// <param name="requestBody">The http request body</param>
        /// <param name="apiType">The API type</param>
        /// <returns>A BaseLunaAPIProp instance</returns>
        public async Task<BaseAPIVersionProp> ParseAndValidateAPIVersionAsync(string requestBody, string apiType)
        {
            BaseAPIVersionProp version = null;
            if (apiType.Equals(LunaAPIType.Realtime.ToString()))
            {
                version = DeserializeRequestBodyAsync<RealtimeEndpointAPIVersionProp>(requestBody);
                RealtimeEndpointAPIVersionType versionType;
                if (!Enum.TryParse<RealtimeEndpointAPIVersionType>(version.Type, out versionType))
                {
                    throw new LunaBadRequestUserException(
                        string.Format(ErrorMessages.VERSION_TYPE_NOT_SUPPORTED, version.Type, apiType),
                        UserErrorCode.InvalidParameter);
                }

                switch (versionType)
                {
                    case RealtimeEndpointAPIVersionType.AzureML:
                        version = DeserializeRequestBodyAsync<AzureMLRealtimeEndpointAPIVersionProp>(requestBody);
                        break;
                    case RealtimeEndpointAPIVersionType.AzureDatabricks:
                        version = DeserializeRequestBodyAsync<AzureDatabricksRealtimeEndpointAPIVersionProp>(requestBody);
                        break;
                    default:
                        throw new LunaBadRequestUserException($"Version type {version.Type} is not supported.",
                            UserErrorCode.InvalidParameter);
                }
            }
            else if (apiType.Equals(LunaAPIType.Pipeline.ToString()))
            {
                version = DeserializeRequestBodyAsync<PipelineEndpointAPIVersionProp>(requestBody);
                PipelineEndpointAPIVersionType versionType;
                if (!Enum.TryParse<PipelineEndpointAPIVersionType>(version.Type, out versionType))
                {
                    throw new LunaBadRequestUserException(
                        string.Format(ErrorMessages.API_TYPE_NOT_SUPPORTED, version.Type),
                        UserErrorCode.InvalidParameter);
                }

                switch (versionType)
                {
                    case PipelineEndpointAPIVersionType.AzureML:
                        version = DeserializeRequestBodyAsync<AzureMLPipelineEndpointAPIVersionProp>(requestBody);
                        break;
                    default:
                        throw new LunaBadRequestUserException(
                            string.Format(ErrorMessages.API_TYPE_NOT_SUPPORTED, version.Type),
                            UserErrorCode.InvalidParameter);

                }
            }
            else
            {
                version = DeserializeRequestBodyAsync<BaseAPIVersionProp>(requestBody);
                throw new LunaBadRequestUserException(
                    string.Format(ErrorMessages.API_TYPE_NOT_SUPPORTED, version.Type),
                    UserErrorCode.InvalidParameter);
            }

            return version;
        }

        private T DeserializeRequestBodyAsync<T>(string requestBody)
        {
            T obj = (T)JsonConvert.DeserializeObject(requestBody, typeof(T), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
            return obj;
        }
    }
}