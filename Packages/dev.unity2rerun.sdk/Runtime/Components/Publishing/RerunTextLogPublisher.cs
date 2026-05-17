// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Components/Publishing
// Purpose: Integrates managed Rerun logging with Unity runtime components.

using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    /// <summary>
    /// Provides Rerun Text Log Publisher support for Unity2Rerun.
    /// </summary>
    [AddComponentMenu("Rerun/Publishers/Rerun Text Log Publisher")]
    public class RerunTextLogPublisher : RerunPublisherBase
    {
        [SerializeField, Tooltip("The log message body.")]
        private string _message = "Unity event";

        [SerializeField, Tooltip("Log level: INFO, WARN, ERROR, DEBUG, TRACE.")]
        private string _level = "INFO";

        [SerializeField, Tooltip("If false, publish once then stop. If true, repeat at publish rate.")]
        private bool _repeat;

        [SerializeField, Tooltip("Append current frame count to the message.")]
        private bool _appendFrameCount;

        private bool _hasPublishedOnce;

        public string Message
        {
            get => _message;
            set => _message = value;
        }

        public string Level
        {
            get => _level;
            set => _level = value;
        }

        public bool Repeat
        {
            get => _repeat;
            set => _repeat = value;
        }

        public bool AppendFrameCount
        {
            get => _appendFrameCount;
            set => _appendFrameCount = value;
        }

        protected override void OnEnable()
        {
            _hasPublishedOnce = false;
            base.OnEnable();
        }

        protected override void PublishNowCore(RerunManager manager, string entityPath)
        {
            var msg = _message;
            if (_appendFrameCount)
                msg += $" frame={Time.frameCount}";

            manager.LogText(entityPath, msg, _level);

            if (!_repeat)
            {
                _hasPublishedOnce = true;
                IsPublishing = false;
            }
        }

        protected override void Update()
        {
            if (_hasPublishedOnce && !_repeat)
                return;

            base.Update();
        }
    }
}
