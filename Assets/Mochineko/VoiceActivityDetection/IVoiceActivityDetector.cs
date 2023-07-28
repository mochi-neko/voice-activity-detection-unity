#nullable enable
using System;
using UniRx;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// Detector of voice activity.
    /// </summary>
    public interface IVoiceActivityDetector : IDisposable
    {
        /// <summary>
        /// Current voice activity.
        /// </summary>
        IReadOnlyReactiveProperty<bool> VoiceIsActive { get; }

        /// <summary>
        /// Updates the state of the detection.
        /// </summary>
        void Update();

        /// <summary>
        /// Sets the detector active or inactive.
        /// </summary>
        /// <param name="isActive"></param>
        void SetDetectorActive(bool isActive);
    }
}