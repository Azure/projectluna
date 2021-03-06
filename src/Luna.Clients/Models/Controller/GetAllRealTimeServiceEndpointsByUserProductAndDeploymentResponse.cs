﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Clients.Models.Controller
{
    public class GetAllRealTimeServiceEndpointsByUserProductAndDeploymentResponse
    {
        public List<Endpoint> endpoints { get; set; }
        public class Endpoint
        {
            public string endpointId { get; set; }
            public string startTimeUtc { get; set; }
            public string completeTimeUtc { get; set; }
            public string scoringUrl { get; set; }
            public string primaryKey { get; set; }
            public string secondaryKey { get; set; }
            public String description { get; set; }
        }
        public GetAllRealTimeServiceEndpointsByUserProductAndDeploymentResponse()
        {
            this.endpoints = new List<Endpoint>();
        }
    }
}
