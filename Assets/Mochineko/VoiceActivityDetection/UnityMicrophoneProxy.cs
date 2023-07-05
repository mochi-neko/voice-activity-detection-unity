#nullable enable
using System;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// A proxy of UnityEngine.Microphone.
    /// </summary>
    public sealed class UnityMicrophoneProxy : IDisposable
    {
        private readonly string? deviceName;
        
        /// <summary>
        /// AudioClip instance of microphone recording.
        /// </summary>
        public AudioClip AudioClip { get; }

        /// <summary>
        /// Creates a new instance of <see cref="UnityMicrophoneProxy"/>.
        /// </summary>
        /// <param name="deviceName">Microphone device name to record, `null` specifies OS default device.</param>
        /// <param name="loopLengthSeconds">Loop time(sec) of AudioClip.</param>
        /// <param name="frequency">Frequency (= sampling rate) of recording.</param>
        public UnityMicrophoneProxy(
            string? deviceName = null,
            int loopLengthSeconds = 1,
            int frequency = 44100)
        {
            this.deviceName = deviceName;
            // NOTE: Because UnityEngine.Microphone updates only latest AudioClip instance, if you want to use multiple recorder, you should use this proxy.
            this.AudioClip = Microphone.Start(this.deviceName, loop: true, loopLengthSeconds, frequency);
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Microphone.End(this.deviceName);
            UnityEngine.Object.Destroy(this.AudioClip);
        }

        /// <summary>
        /// Get current sample position of microphone recording in looped AudioClip.
        /// </summary>
        /// <returns></returns>
        public int GetSamplePosition()
            => Microphone.GetPosition(this.deviceName);
    }
}