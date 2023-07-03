#nullable enable
using System;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// Source of voice data.
    /// </summary>
    public interface IVoiceSource : IDisposable
    {
        int SamplingRate { get; }
        
        int Channels { get; }
        
        /// <summary>
        /// Called when a segment is read.
        /// </summary>
        IObservable<VoiceSegment> OnSegmentRead { get; }
        
        /// <summary>
        /// Updates the state of the source.
        /// </summary>
        void Update();
    }
}