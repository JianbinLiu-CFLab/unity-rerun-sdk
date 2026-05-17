// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: UnityProject/Assets/TutorialInfo/Scripts
// Purpose: Supports the Unity tutorial information panel used by the validation project.

using System;
using UnityEngine;
/// <summary>
/// Stores tutorial metadata shown by the Unity validation project.
/// </summary>
public class Readme : ScriptableObject
{
    public Texture2D icon;
    public string title;
    public Section[] sections;
    public bool loadedLayout;
    /// <summary>
    /// Stores tutorial metadata shown by the Unity validation project.
    /// </summary>
    [Serializable]
    public class Section
    {
        public string heading, text, linkText, url;
    }
}
