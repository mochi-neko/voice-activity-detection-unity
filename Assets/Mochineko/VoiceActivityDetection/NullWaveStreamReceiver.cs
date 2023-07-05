#nullable enable
using System.IO;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// Null object of <see cref="IWaveStreamReceiver"/> that do nothing.
    /// </summary>
    public sealed class NullWaveStreamReceiver : IWaveStreamReceiver
    {
        void IWaveStreamReceiver.OnReceive(Stream stream)
        {
            stream.Dispose();
        }
    }
}