#nullable enable
using System;

namespace Mochineko.VoiceActivityDetection
{
    public interface IVoiceSource : IDisposable
    {
        void Update();
        IObservable<VoiceSegment> OnBufferRead { get; }
    }
}