#nullable enable
using System;

namespace Mochineko.VoiceActivityDetection
{
    public interface IVoiceInput : IDisposable
    {
        void Update();
        IObservable<(float[] buffer, int lenght)> OnBufferRead { get; }
    }
}