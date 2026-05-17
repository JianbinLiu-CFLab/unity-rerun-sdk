// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Components/Publishing
// Purpose: Integrates managed Rerun logging with Unity runtime components.

using Unity.RerunSDK.Core;
using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    /// <summary>
    /// Provides Rerun Publisher Base support for Unity2Rerun.
    /// </summary>
    public abstract class RerunPublisherBase : MonoBehaviour
    {
        [SerializeField, Tooltip("Target RerunManager. Leave empty to auto-detect in the scene.")]
        protected RerunManager _manager;

        [SerializeField, Tooltip("Entity path in the Rerun recording. Leave empty to derive from GameObject.")]
        protected string _entityPath;

        [SerializeField, Tooltip("Publish rate in Hz. 0 or negative = every frame.")]
        protected float _publishRateHz = 10f;

        [SerializeField, Tooltip("Start publishing automatically when enabled.")]
        protected bool _publishOnEnable = true;

        [SerializeField, Tooltip("Log a warning if no RerunManager is found.")]
        protected bool _warnIfManagerMissing = true;

        private RerunPublishRateLimiter _rateLimiter = new();
        private bool _hasWarnedManagerMissing;

        protected bool IsPublishing { get; set; }

        public RerunManager Manager
        {
            get => _manager;
            set => _manager = value;
        }

        public string EntityPath
        {
            get => _entityPath;
            set => _entityPath = value;
        }

        public float PublishRateHz
        {
            get => _publishRateHz;
            set
            {
                _publishRateHz = value;
                ResetRateLimiter();
            }
        }

        public bool PublishOnEnable
        {
            get => _publishOnEnable;
            set => _publishOnEnable = value;
        }

        public bool WarnIfManagerMissing
        {
            get => _warnIfManagerMissing;
            set => _warnIfManagerMissing = value;
        }

        protected virtual void OnEnable()
        {
            if (_publishOnEnable)
                IsPublishing = true;
        }

        protected virtual void OnDisable()
        {
            IsPublishing = false;
        }

        protected virtual void Update()
        {
            if (!IsPublishing)
                return;

            var manager = ResolveManager();
            if (manager == null || !manager.IsRecording)
                return;

            if (!_rateLimiter.CanPublish(_publishRateHz, Time.unscaledTimeAsDouble))
                return;

            var path = ResolveEntityPath();
            PublishNowCore(manager, path);
        }

        /// Override in derived publishers to perform the actual log call.
        protected abstract void PublishNowCore(RerunManager manager, string entityPath);

        /// Manual publish, e.g. from a UI button or sample script.
        public void PublishOnce()
        {
            var manager = ResolveManager();
            if (manager == null || !manager.IsRecording)
                return;

            PublishNowCore(manager, ResolveEntityPath());
        }

        private RerunManager ResolveManager()
        {
            if (_manager != null)
                return _manager;

            var found = CompatibilityFind();
            if (found == null && _warnIfManagerMissing && !_hasWarnedManagerMissing)
            {
                Debug.LogWarning($"[Rerun] {GetType().Name}: No RerunManager found in scene. " +
                    "Add a RerunManager to the scene or assign it explicitly in the Inspector.");
                _hasWarnedManagerMissing = true;
            }
            return found;
        }

        private static RerunManager CompatibilityFind()
        {
#if UNITY_2023_1_OR_NEWER
            return FindFirstObjectByType<RerunManager>();
#else
            return FindObjectOfType<RerunManager>();
#endif
        }

        private string ResolveEntityPath()
        {
            if (!string.IsNullOrEmpty(_entityPath))
                return _entityPath;

            return RerunEntityPath.FromGameObject(ResolveDefaultEntityPathGameObject()).Value;
        }

        protected virtual GameObject ResolveDefaultEntityPathGameObject()
        {
            return gameObject;
        }

        protected void ResetRateLimiter()
        {
            _rateLimiter.Reset();
        }
    }
}
