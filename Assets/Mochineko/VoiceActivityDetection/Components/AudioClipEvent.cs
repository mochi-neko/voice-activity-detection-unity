#nullable enable
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Mochineko.VoiceActivityDetection.Components
{
    /// <summary>
    /// Implements UnityEvent&lt;AudioClip&gt; to serialize generics type.
    /// </summary>
    [Serializable]
    public sealed class AudioClipEvent : UnityEvent<AudioClip>
    {
    }
}
