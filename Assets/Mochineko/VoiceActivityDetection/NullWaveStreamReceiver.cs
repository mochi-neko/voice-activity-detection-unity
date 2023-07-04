#nullable enable
using System.IO;

namespace Mochineko.VoiceActivityDetection
{
    public sealed class NullWaveStreamReceiver : IWaveStreamReceiver
    {
        public void OnReceive(Stream stream)
        {
            stream.Dispose();
        }
    }
}