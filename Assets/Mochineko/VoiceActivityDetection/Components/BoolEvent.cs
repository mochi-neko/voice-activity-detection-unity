#nullable enable
using System;
using UnityEngine.Events;

namespace Mochineko.VoiceActivityDetection.Components
{
    [Serializable]
    public sealed class BoolEvent : UnityEvent<bool>
    {
    }
}
