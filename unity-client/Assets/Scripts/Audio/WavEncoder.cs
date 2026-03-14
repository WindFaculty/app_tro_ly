using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace LocalAssistant.Audio
{
    public static class WavEncoder
    {
        public static byte[] Encode(AudioClip clip)
        {
            if (clip == null)
            {
                return Array.Empty<byte>();
            }

            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            var pcmBytes = new byte[samples.Length * 2];
            var pcmIndex = 0;
            for (var index = 0; index < samples.Length; index++)
            {
                var value = (short)Mathf.Clamp(samples[index] * short.MaxValue, short.MinValue, short.MaxValue);
                var sampleBytes = BitConverter.GetBytes(value);
                pcmBytes[pcmIndex++] = sampleBytes[0];
                pcmBytes[pcmIndex++] = sampleBytes[1];
            }

            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + pcmBytes.Length);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((short)(clip.channels * 2));
            writer.Write((short)16);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(pcmBytes.Length);
            writer.Write(pcmBytes);
            writer.Flush();
            return stream.ToArray();
        }
    }
}
