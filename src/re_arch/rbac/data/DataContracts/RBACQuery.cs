﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Luna.RBAC.Data.DataContracts
{
    public class RBACQuery
    {
        public string Uid { get; set; }

        public string ResourceId { get; set; }

        public string Action { get; set; }
    }
}
