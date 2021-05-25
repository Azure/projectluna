﻿using Luna.Common.Utils;
using Luna.RBAC.Public.Client.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Luna.RBAC.Public.Client.DataContracts
{
    public class RoleAssignment
    {
        public static string example = JsonConvert.SerializeObject(new RoleAssignment()
        {
            Uid = Guid.NewGuid().ToString(),
            UserName = "FirstName LastName",
            Role = RBACRole.SystemAdmin.ToString()
        });

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            ValidationUtils.ValidateObjectId(Uid, nameof(Uid));
            ValidationUtils.ValidateObjectId(UserName, nameof(UserName));
            ValidationUtils.ValidateEnum(Role, typeof(RBACRole), nameof(Role));
        }

        [JsonProperty(PropertyName = "Uid", Required = Required.Always)]
        public string Uid { get; set; }

        [JsonProperty(PropertyName = "UserName", Required = Required.Always)]
        public string UserName { get; set; }

        [JsonProperty(PropertyName = "Role", Required = Required.Always)]
        public string Role { get; set; }

        [JsonProperty(PropertyName = "CreatedTime", Required = Required.Default)]
        public DateTime CreatedTime { get; set; }
    }
}
