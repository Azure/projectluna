﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.Data.DataContracts.Luna.AI
{
    public class AMLArtifactsResponse
    {
        public List<Dictionary<string, object>> value { get; set; }
    }
}
