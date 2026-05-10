// SPDX-License-Identifier: Apache-2.0

using System;

namespace Unity.RerunSDK.Unity
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class RerunTransformAttribute : Attribute
    {
        public RerunTransformAttribute(string entityPath)
        {
            EntityPath = entityPath;
        }

        public string EntityPath { get; }
        public float RateHz { get; set; } = 10f;
    }
}
