#nullable enable
using System;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// Source of voice data.
    /// </summary>
    public interface IVoiceSource : IDisposable
    {
        /// <summary>
        /// Sampling rate (= frequency) of voice data.
        /// </summary>
        int SamplingRate { get; }
        
        /// <summary>
        /// Channels count of voice data.
        /// </summary>
        int Channels { get; }
        
        /// <summary>
        /// Called when a segment has been read.
        /// </summary>
        IObservable<VoiceSegment> OnSegmentRead { get; }
        
        /// <summary>
        /// Updates the state of the source.
        /// </summary>
        void Update();
    }
}