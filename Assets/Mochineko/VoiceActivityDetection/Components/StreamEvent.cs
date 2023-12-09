#nullable enable
using System;
using System.IO;
using UnityEngine.Events;

namespace Mochineko.VoiceActivityDetection.Components
{
    [Serializable]
    public sealed class StreamEvent : UnityEvent<Stream>
    {
    }
}
