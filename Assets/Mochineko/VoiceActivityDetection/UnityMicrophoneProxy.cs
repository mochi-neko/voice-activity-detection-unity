#nullable enable
using System;
using System.Linq;
using Unity.Logging;
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
        /// Loop time(sec) of microphone recording.
        /// </summary>
        public int LoopLengthSeconds { get; }

        /// <summary>
        /// Creates a new instance of <see cref="UnityMicrophoneProxy"/>.
        /// </summary>
        /// <param name="deviceName">Microphone device name to record, `null` specifies OS default device.</param>
        /// <param name="loopLengthSeconds">Loop time(sec) of AudioClip.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public UnityMicrophoneProxy(
            string? deviceName = null,
            int loopLengthSeconds = 1)
        {
            if (!Microphone.devices.Any())
            {
                throw new InvalidOperationException("No microphone device found.");
            }

            if (loopLengthSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(loopLengthSeconds), loopLengthSeconds, "Loop length must be greater than 0.");
            }

            this.deviceName = deviceName;
            this.LoopLengthSeconds = loopLengthSeconds;

            if (this.deviceName == null)
            {
                Log.Debug("[VAD] Use default microphone device: {0}.", Microphone.devices[0]);
            }

            // NOTE: Because UnityEngine.Microphone updates only latest AudioClip instance, if you want to use multiple recorder, you should use this proxy.
            this.AudioClip = Microphone.Start(this.deviceName, loop: true, loopLengthSeconds, GetMaxFrequency());
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
        /// Gets current sample position of microphone recording in looped AudioClip.
        /// </summary>
        /// <returns></returns>
        public int GetSamplePosition()
            => Microphone.GetPosition(this.deviceName);

        /// <summary>
        /// Gets maximum frequency of recording microphone device.
        /// </summary>
        /// <returns></returns>
        public int GetMaxFrequency()
        {
            Microphone.GetDeviceCaps(deviceName, out _, out var maxFrequency);

            return maxFrequency;
        }
    }
}
