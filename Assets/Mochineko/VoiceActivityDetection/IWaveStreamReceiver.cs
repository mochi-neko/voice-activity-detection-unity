#nullable enable
using System.IO;

namespace Mochineko.VoiceActivityDetection
{
    /// <summary>
    /// Receiver of wave stream.
    /// </summary>
    public interface IWaveStreamReceiver
    {
        /// <summary>
        /// Receives wave stream.
        /// Notice that stream instance should be disposed by the receiver.
        /// </summary>
        /// <param name="stream">Received stream</param>
        void OnReceive(Stream stream);
    }
}