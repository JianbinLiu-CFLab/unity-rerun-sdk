// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Unity/Attributes
// Purpose: Defines attributes consumed by generated Rerun logging code.

using System;

namespace Unity.RerunSDK.Unity
{
    /// <summary>
    /// Provides Rerun Transform Attribute support for Unity2Rerun.
    /// </summary>
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
