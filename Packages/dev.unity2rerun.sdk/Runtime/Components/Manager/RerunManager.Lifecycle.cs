// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Components/Manager
// Purpose: Owns Unity lifecycle callbacks for the Rerun manager component.

using Unity.RerunSDK.Encoding;
using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    public partial class RerunManager
    {
        private void Awake()
        {
            if (_runInBackground)
                Application.runInBackground = true;

            MigrateLegacyOutputPath();
            _encoder = new ManagedRerunEncoder();
        }

        private void OnValidate()
        {
            MigrateLegacyOutputPath();
        }

        private void Start()
        {
            if (_recordOnStart)
                StartRecording();
        }

        private void Update()
        {
            DiscoverGeneratedLogSourcesIfDue();
            DriveGeneratedLogSources();
        }

        private void OnDestroy()
        {
            if (IsRecording) StopRecording();
        }
    }
}
