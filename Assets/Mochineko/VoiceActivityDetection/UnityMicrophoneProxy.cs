#nullable enable
using System;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection
{
    public sealed class UnityMicrophoneProxy : IDisposable
    {
        private readonly string? deviceName;
        public AudioClip AudioClip { get; }
        
        public UnityMicrophoneProxy(string? deviceName = null, int loopLengthSeconds = 1, int frequency = 44100)
        {
            this.deviceName = deviceName;
            // NOTE: Because UnityEngine.Microphone updates only latest AudioClip instance, if you want to use multiple recorder, you should use this proxy.
            this.AudioClip = Microphone.Start(this.deviceName, loop:true, loopLengthSeconds, frequency);
        }
        
        public void Dispose()
        {
            Microphone.End(this.deviceName);
            UnityEngine.Object.Destroy(this.AudioClip);
        }

        public int GetSamplePosition()
            => Microphone.GetPosition(this.deviceName);
    }
}
