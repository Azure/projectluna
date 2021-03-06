﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Clients.Models.Controller.Backend
{
    public class OperationStatus
    {
        public string runUuid { get; set; }
        public string rootRunUuid { get; set; }
        public string status { get; set; }
        public string startTimeUtc { get; set; }
        public string endTimeUtc { get; set; }
        public String description { get; set; }
        public object error { get; set; }
    }
}
