#nullable enable
namespace Mochineko.VoiceActivityDetection
{
    public readonly struct VoiceSegment
    {
        public readonly float[] buffer;
        public readonly int length;

        public VoiceSegment(float[] buffer, int length)
        {
            this.buffer = buffer;
            this.length = length;
        }
    }
}