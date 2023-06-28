#nullable enable
using System.IO;
using NAudio.Utils;
using NAudio.Wave;
using UniRx;
using UnityEngine;

namespace Mochineko.VoiceActivityDetection
{
    public sealed class MicrophoneRecorder : MonoBehaviour
    {
        private IMicrophoneReader? reader;
        private WaveFileWriter? writer;
        private int offset;

        private void Start()
        {
            reader = new UnityMicrophoneReader();

            var outputFileStream = File.OpenWrite("C:/Users/mochineko/Desktop/output.wav");

            var outputStream = new IgnoreDisposeStream(outputFileStream);

            var format = new WaveFormat(
                rate: 44100,
                bits: 16,
                channels: 1);
            
            writer = new WaveFileWriter(outputStream, format);
            
            reader
                .OnBufferRead
                .Subscribe(OnBufferRead)
                .AddTo(this);
        }

        private void OnDestroy()
        {
            writer?.Dispose();
            reader?.Dispose();
        }

        private void OnBufferRead((float[] buffer, int lenght) value)
        {
            if (writer == null)
            {
                return;
            }
            
            writer.WriteSamples(value.buffer, offset, count: value.lenght);
            offset += value.lenght;
        }
    }
}