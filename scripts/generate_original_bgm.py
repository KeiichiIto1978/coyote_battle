"""Coyote Battle用のループ可能なオリジナルBGMを決定的に生成する。"""

from __future__ import annotations

import math
import struct
import wave
from pathlib import Path


SAMPLE_RATE = 22_050
CHANNELS = 2
BEATS_PER_MINUTE = 120
BEAT_SECONDS = 60.0 / BEATS_PER_MINUTE
BARS = 48
BEATS_PER_BAR = 4
DURATION_SECONDS = BARS * BEATS_PER_BAR * BEAT_SECONDS
OUTPUT_PATH = (
    Path(__file__).resolve().parents[1]
    / "Assets"
    / "CoyoteBattle"
    / "Resources"
    / "Audio"
    / "CoyoteBattleTheme.wav"
)

NOTE_OFFSETS = {
    "C": 0,
    "C#": 1,
    "D": 2,
    "D#": 3,
    "E": 4,
    "F": 5,
    "F#": 6,
    "G": 7,
    "G#": 8,
    "A": 9,
    "A#": 10,
    "B": 11,
}

# 8小節の和声を6周し、最後のAから冒頭のDmへ自然に解決させる。
CHORDS = (
    ("D3", "F3", "A3"),
    ("A#2", "D3", "F3"),
    ("C3", "E3", "G3"),
    ("A2", "C#3", "E3"),
    ("D3", "F3", "A3"),
    ("G2", "A#2", "D3"),
    ("A#2", "D3", "F3"),
    ("A2", "C#3", "E3"),
)

# Dマイナー・ペンタトニックを中心にした独自旋律。特定楽曲は参照しない。
MELODY = (
    "D5",
    "F5",
    "G5",
    "A5",
    "C6",
    "A5",
    "G5",
    "F5",
    "D5",
    "G5",
    "A5",
    "C6",
    "D6",
    "C6",
    "A5",
    "G5",
)


def frequency(note: str) -> float:
    """音名を平均律の周波数へ変換する。"""
    octave = int(note[-1])
    name = note[:-1]
    midi = 12 * (octave + 1) + NOTE_OFFSETS[name]
    return 440.0 * (2.0 ** ((midi - 69) / 12.0))


def envelope(local_time: float, duration: float, attack: float, release: float) -> float:
    """クリックノイズを避けるアタック・リリース包絡を返す。"""
    if local_time < 0.0 or local_time >= duration:
        return 0.0
    attack_gain = min(1.0, local_time / attack) if attack > 0.0 else 1.0
    remaining = duration - local_time
    release_gain = min(1.0, remaining / release) if release > 0.0 else 1.0
    return attack_gain * release_gain


def piano_tone(freq: float, local_time: float, duration: float) -> float:
    """減衰する倍音でピアノ風の音色を合成する。"""
    amp = envelope(local_time, duration, 0.008, 0.045)
    amp *= math.exp(-3.8 * local_time / duration)
    phase = math.tau * freq * local_time
    return amp * (
        math.sin(phase)
        + 0.45 * math.sin(phase * 2.0)
        + 0.18 * math.sin(phase * 3.0)
        + 0.08 * math.sin(phase * 4.0)
    ) / 1.71


def synth_pad(freq: float, local_time: float) -> tuple[float, float]:
    """左右でわずかに位相を変えた柔らかなシンセパッドを合成する。"""
    amp = envelope(local_time, BEATS_PER_BAR * BEAT_SECONDS, 0.16, 0.16)
    phase = math.tau * freq * local_time
    left = math.sin(phase) + 0.32 * math.sin(phase * 2.003)
    right = math.sin(phase * 1.002) + 0.32 * math.sin(phase * 1.997)
    return amp * left / 1.32, amp * right / 1.32


def deterministic_noise(sample_index: int) -> float:
    """外部乱数へ依存しない打楽器用ノイズを返す。"""
    value = math.sin(sample_index * 12.9898 + 78.233) * 43_758.5453
    return 2.0 * (value - math.floor(value)) - 1.0


def render_sample(sample_index: int) -> tuple[int, int]:
    """指定サンプル位置のステレオPCM値を生成する。"""
    time = sample_index / SAMPLE_RATE
    beat_position = time / BEAT_SECONDS
    bar_index = min(BARS - 1, int(beat_position // BEATS_PER_BAR))
    beat_in_bar = beat_position - bar_index * BEATS_PER_BAR
    chord = CHORDS[bar_index % len(CHORDS)]
    bar_time = beat_in_bar * BEAT_SECONDS

    left = 0.0
    right = 0.0

    for chord_note in chord:
        pad_left, pad_right = synth_pad(frequency(chord_note), bar_time)
        left += 0.11 * pad_left
        right += 0.11 * pad_right

    eighth_index = min(7, int(beat_in_bar * 2.0))
    eighth_time = (beat_in_bar * 2.0 - eighth_index) * (BEAT_SECONDS / 2.0)
    arpeggio_note = chord[(eighth_index * 2 + bar_index) % len(chord)]
    arpeggio_frequency = frequency(arpeggio_note) * (2.0 if eighth_index % 3 else 1.0)
    arpeggio = piano_tone(arpeggio_frequency, eighth_time, BEAT_SECONDS / 2.0)
    pan = -0.25 if eighth_index % 2 == 0 else 0.25
    left += 0.26 * arpeggio * (1.0 - pan)
    right += 0.26 * arpeggio * (1.0 + pan)

    phrase_index = bar_index // len(CHORDS)
    melody_index = (bar_index * 2 + eighth_index // 4 + phrase_index * 3) % len(MELODY)
    melody_frequency = frequency(MELODY[melody_index])
    melody = piano_tone(melody_frequency, eighth_time, BEAT_SECONDS / 2.0)
    melody_gain = 0.18 if eighth_index in (0, 3, 4, 6) else 0.08
    left += melody_gain * melody * 1.12
    right += melody_gain * melody * 0.88

    beat_index = min(3, int(beat_in_bar))
    beat_time = (beat_in_bar - beat_index) * BEAT_SECONDS
    bass_note = chord[0] if beat_index != 2 else chord[2]
    bass = piano_tone(frequency(bass_note) / 2.0, beat_time, BEAT_SECONDS)
    left += 0.22 * bass
    right += 0.22 * bass

    if beat_index in (0, 2) and beat_time < 0.22:
        drum_envelope = envelope(beat_time, 0.22, 0.003, 0.08)
        drum = math.sin(math.tau * (72.0 - 30.0 * beat_time) * beat_time)
        drum += 0.12 * deterministic_noise(sample_index)
        left += 0.20 * drum_envelope * drum
        right += 0.20 * drum_envelope * drum

    shaker_step = min(7, int(beat_in_bar * 2.0))
    shaker_time = (beat_in_bar * 2.0 - shaker_step) * (BEAT_SECONDS / 2.0)
    if shaker_time < 0.06:
        shaker_envelope = envelope(shaker_time, 0.06, 0.002, 0.04)
        shaker = deterministic_noise(sample_index + 17) * shaker_envelope
        left += 0.035 * shaker
        right -= 0.035 * shaker

    master_gain = 0.72
    left_pcm = int(max(-1.0, min(1.0, left * master_gain)) * 32_767)
    right_pcm = int(max(-1.0, min(1.0, right * master_gain)) * 32_767)
    return left_pcm, right_pcm


def generate() -> None:
    """96秒のステレオWAVを生成し、ループ境界の無音・段差を検証する。"""
    OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    total_samples = int(DURATION_SECONDS * SAMPLE_RATE)
    first_sample: tuple[int, int] | None = None
    last_sample: tuple[int, int] | None = None

    with wave.open(str(OUTPUT_PATH), "wb") as output:
        output.setnchannels(CHANNELS)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        buffer = bytearray()
        for sample_index in range(total_samples):
            sample = render_sample(sample_index)
            if first_sample is None:
                first_sample = sample
            last_sample = sample
            buffer.extend(struct.pack("<hh", *sample))
            if len(buffer) >= 65_536:
                output.writeframesraw(buffer)
                buffer.clear()
        if buffer:
            output.writeframesraw(buffer)

    assert first_sample is not None and last_sample is not None
    boundary_jump = max(abs(first_sample[i] - last_sample[i]) for i in range(CHANNELS))
    if boundary_jump > 256:
        raise RuntimeError(f"ループ境界のPCM段差が大きすぎます: {boundary_jump}")

    print(
        f"generated: {OUTPUT_PATH}\n"
        f"duration: {DURATION_SECONDS:.1f}s, sample_rate: {SAMPLE_RATE}Hz, "
        f"boundary_jump: {boundary_jump}"
    )


if __name__ == "__main__":
    generate()
