#nullable enable
using System;
using UniRx;

namespace Mochineko.VoiceActivityDetection
{
    public interface IVoiceActivityDetection : IDisposable
    {
        IReadOnlyReactiveProperty<bool> IsActive { get; }
        void Update();
    }
}