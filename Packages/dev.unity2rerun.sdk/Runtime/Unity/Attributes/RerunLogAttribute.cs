// SPDX-License-Identifier: Apache-2.0

using System;

namespace Unity.RerunSDK.Unity
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class RerunLogAttribute : Attribute
    {
        public RerunLogAttribute(string entityPath)
        {
            EntityPath = entityPath;
        }

        public string EntityPath { get; }
        public float RateHz { get; set; } = 10f;
        public string Level { get; set; } = "INFO";
    }
}
