using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CoyoteBattle.Tests.Presentation
{
    /// <summary>
    /// 同梱するオリジナルBGMの長さとAndroid向け読込方式を保証します。
    /// </summary>
    public sealed class OriginalBgmAssetTests
    {
        private const string BattleAssetPath =
            "Assets/CoyoteBattle/Resources/Audio/CoyoteBattleTheme.wav";
        private const string TitleAssetPath =
            "Assets/CoyoteBattle/Resources/Audio/CoyoteBattleTitleTheme.wav";
        private const int WavHeaderBytes = 44;

        /// <summary>
        /// Title曲とプレイ曲が90〜120秒で、長時間再生向けのStreaming設定になっていることを保証します。
        /// </summary>
        [TestCase(TitleAssetPath)]
        [TestCase(BattleAssetPath)]
        public void OriginalBgm_同梱音源_90秒以上120秒以下でStreaming読込する(string assetPath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;

            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.length, Is.InRange(90f, 120f));
            Assert.That(importer, Is.Not.Null);
            Assert.That(
                importer.defaultSampleSettings.loadType,
                Is.EqualTo(AudioClipLoadType.Streaming)
            );
            Assert.That(
                importer.defaultSampleSettings.compressionFormat,
                Is.EqualTo(AudioCompressionFormat.Vorbis)
            );
            Assert.That(importer.defaultSampleSettings.quality, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(importer.loadInBackground, Is.True);
            Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.False);
        }

        /// <summary>
        /// 同梱WAVの先頭と末尾に段差がなく、境界直前直後が長い無音にならないことを保証します。
        /// </summary>
        [TestCase(TitleAssetPath)]
        [TestCase(BattleAssetPath)]
        public void OriginalBgm_ループ境界_PCM段差と目立つ無音がない(string assetPath)
        {
            var wav = File.ReadAllBytes(assetPath);
            var channels = BitConverter.ToInt16(wav, 22);
            var sampleRate = BitConverter.ToInt32(wav, 24);
            var bytesPerFrame = channels * sizeof(short);
            var frameCount = (wav.Length - WavHeaderBytes) / bytesPerFrame;
            var boundaryWindowFrames = sampleRate / 20;
            var firstLeft = BitConverter.ToInt16(wav, WavHeaderBytes);
            var firstRight = BitConverter.ToInt16(wav, WavHeaderBytes + sizeof(short));
            var lastFrameOffset = WavHeaderBytes + (frameCount - 1) * bytesPerFrame;
            var lastLeft = BitConverter.ToInt16(wav, lastFrameOffset);
            var lastRight = BitConverter.ToInt16(wav, lastFrameOffset + sizeof(short));

            Assert.That(channels, Is.EqualTo(2));
            Assert.That(Math.Abs(firstLeft - lastLeft), Is.LessThanOrEqualTo(256));
            Assert.That(Math.Abs(firstRight - lastRight), Is.LessThanOrEqualTo(256));
            Assert.That(CalculateRms(wav, 0, boundaryWindowFrames, channels), Is.GreaterThan(100d));
            Assert.That(
                CalculateRms(
                    wav,
                    frameCount - boundaryWindowFrames,
                    boundaryWindowFrames,
                    channels
                ),
                Is.GreaterThan(100d)
            );
        }

        private static double CalculateRms(byte[] wav, int startFrame, int frameCount, int channels)
        {
            var sumOfSquares = 0d;
            var sampleCount = frameCount * channels;
            var offset = WavHeaderBytes + startFrame * channels * sizeof(short);
            for (var index = 0; index < sampleCount; index++)
            {
                var sample = BitConverter.ToInt16(wav, offset + index * sizeof(short));
                sumOfSquares += sample * (double)sample;
            }

            return Math.Sqrt(sumOfSquares / sampleCount);
        }
    }
}
