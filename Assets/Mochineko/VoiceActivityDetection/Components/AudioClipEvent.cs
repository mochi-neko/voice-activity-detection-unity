#nullable enable
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Mochineko.VoiceActivityDetection.Components
{
    [Serializable]
    public sealed class AudioClipEvent : UnityEvent<AudioClip>
    {
    }
}
