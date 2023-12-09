#nullable enable
using System;
using System.IO;
using UnityEngine.Events;

namespace Mochineko.VoiceActivityDetection.Components
{
    /// <summary>
    /// Implements UnityEvent&lt;Stream&gt; to serialize generics type.
    /// </summary>
    [Serializable]
    public sealed class StreamEvent : UnityEvent<Stream>
    {
    }
}
