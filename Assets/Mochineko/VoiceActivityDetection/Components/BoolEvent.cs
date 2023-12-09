#nullable enable
using System;
using UnityEngine.Events;

namespace Mochineko.VoiceActivityDetection.Components
{
    /// <summary>
    /// Implements UnityEvent&lt;bool&gt; to serialize generics type.
    /// </summary>
    [Serializable]
    public sealed class BoolEvent : UnityEvent<bool>
    {
    }
}
