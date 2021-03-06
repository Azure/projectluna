﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Clients.Models.Controller
{
    public class GetADeployOperationByEndpointIdUserResponse
    {
        public string operationType { get; set; }
        public string endpointId { get; set; }
        public string status { get; set; }
        public string startTimeUtc { get; set; }
        public string completeTimeUtc { get; set; }
        public Object error { get; set; }
    }
}
