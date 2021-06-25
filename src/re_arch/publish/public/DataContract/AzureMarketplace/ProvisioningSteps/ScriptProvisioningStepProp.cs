﻿using Luna.Common.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Luna.Publish.Public.Client
{
    public class ScriptProvisioningStepProp : BaseProvisioningStepProp
    {
        public ScriptProvisioningStepProp()
        {
        }

        [OnDeserialized]
        internal new void OnDeserializedMethod(StreamingContext context)
        {
            ValidationUtils.ValidateHttpsUrl(ScriptPackageUrl, nameof(ScriptPackageUrl));
            base.OnDeserializedMethod(context);
        }

        [JsonProperty(PropertyName = "ScriptPackageUrl", Required = Required.Always)]
        public string ScriptPackageUrl { get; set; }

        [JsonProperty(PropertyName = "EntryScriptFileName", Required = Required.Always)]
        public string EntryScriptFileName { get; set; }

        [JsonProperty(PropertyName = "ResultFileName", Required = Required.Always)]
        public string ResultFileName { get; set; }

        [JsonProperty(PropertyName = "TimeoutInSeconds", Required = Required.Always)]
        public int TimeoutInSeconds { get; set; }

    }
}