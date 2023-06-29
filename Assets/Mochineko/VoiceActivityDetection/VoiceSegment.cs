#nullable enable
namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// Voice segment data.
    /// </summary>
    public readonly struct VoiceSegment
    {
        /// <summary>
        /// Buffer array of voice segment.
        /// </summary>
        public readonly float[] buffer;
        
        /// <summary>
        /// Effective length of voice segment data in buffer.
        /// </summary>
        public readonly int length;

        public VoiceSegment(float[] buffer, int length)
        {
            this.buffer = buffer;
            this.length = length;
        }
    }
}