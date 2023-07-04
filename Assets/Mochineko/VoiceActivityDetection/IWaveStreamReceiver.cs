#nullable enable
using System.IO;

namespace Mochineko.VoiceActivityDetection
{
    public interface IWaveStreamReceiver
    {
        void OnReceive(Stream stream);
    }
}