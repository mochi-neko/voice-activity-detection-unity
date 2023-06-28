#nullable enable
using System;

namespace Mochineko.VoiceActivityDetection
{
    interface IMicrophoneReader : IDisposable
    {
        void Update();
        IObservable<(float[] buffer, int lenght)> OnBufferRead { get; }
    }
}