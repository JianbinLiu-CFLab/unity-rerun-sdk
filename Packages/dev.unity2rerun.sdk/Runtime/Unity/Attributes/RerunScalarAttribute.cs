// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Unity/Attributes
// Purpose: Defines attributes consumed by generated Rerun logging code.

using System;

namespace Unity.RerunSDK.Unity
{
    /// <summary>
    /// Provides Rerun Scalar Attribute support for Unity2Rerun.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class RerunScalarAttribute : Attribute
    {
        public RerunScalarAttribute(string entityPath)
        {
            EntityPath = entityPath;
        }

        public string EntityPath { get; }
        public float RateHz { get; set; } = 10f;
    }
}
