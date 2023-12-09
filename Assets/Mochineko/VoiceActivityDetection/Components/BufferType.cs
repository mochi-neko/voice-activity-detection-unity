#nullable enable
namespace Mochineko.VoiceActivityDetection.Components
{
    /// <summary>
    /// Buffer type of voice activity detection.
    /// </summary>
    public enum BufferType
    {
        /// <summary>
        /// No buffer (flag only).
        /// </summary>
        None,
        /// <summary>
        /// UnityEngine.AudioClip buffer.
        /// </summary>
        AudioClip,
        /// <summary>
        /// Wave file stream buffer.
        /// </summary>
        WaveFileStream,
    }
}
