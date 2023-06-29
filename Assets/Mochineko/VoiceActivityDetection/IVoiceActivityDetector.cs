#nullable enable
using System;
using UniRx;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// Detects voice activity.
    /// </summary>
    public interface IVoiceActivityDetector : IDisposable
    {
        /// <summary>
        /// Current voice activity.
        /// </summary>
        IReadOnlyReactiveProperty<bool> IsActive { get; }
        
        /// <summary>
        /// Updates the state of the detection.
        /// </summary>
        void Update();
    }
}