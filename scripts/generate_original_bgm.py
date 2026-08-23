"""Coyote Battle用のループ可能なオリジナルBGMを決定的に生成する。"""

from __future__ import annotations

import math
import struct
import wave
from collections.abc import Callable
from pathlib import Path


SAMPLE_RATE = 22_050
CHANNELS = 2
BEATS_PER_MINUTE = 120
BEAT_SECONDS = 60.0 / BEATS_PER_MINUTE
BARS = 48
BEATS_PER_BAR = 4
DURATION_SECONDS = BARS * BEATS_PER_BAR * BEAT_SECONDS
OUTPUT_DIRECTORY = (
    Path(__file__).resolve().parents[1]
    / "Assets"
    / "CoyoteBattle"
    / "Resources"
    / "Audio"
)
BATTLE_OUTPUT_PATH = OUTPUT_DIRECTORY / "CoyoteBattleTheme.wav"
TITLE_OUTPUT_PATH = OUTPUT_DIRECTORY / "CoyoteBattleTitleTheme.wav"
TITLE_BEATS_PER_MINUTE = 75
TITLE_BEAT_SECONDS = 60.0 / TITLE_BEATS_PER_MINUTE
TITLE_BARS = 30
TITLE_DURATION_SECONDS = TITLE_BARS * BEATS_PER_BAR * TITLE_BEAT_SECONDS

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

# プレイ曲と対照的な、明るく静かな5小節のTitle用循環を6周する。
TITLE_CHORDS = (
    ("C3", "E3", "G3", "B3"),
    ("A2", "C3", "E3", "G3"),
    ("F2", "A2", "C3", "E3"),
    ("D3", "F3", "A3", "C4"),
    ("G2", "C3", "D3", "G3"),
)

TITLE_MELODY = (
    "C5",
    "E5",
    "G5",
    "A5",
    "G5",
    "E5",
    "D5",
    "C5",
    "A4",
    "D5",
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


def ambient_pad(freq: float, local_time: float, duration: float) -> tuple[float, float]:
    """長い立ち上がりと余韻を持つTitle用の穏やかなパッドを合成する。"""
    amp = envelope(local_time, duration, 0.65, 0.25)
    phase = math.tau * freq * local_time
    left = math.sin(phase) + 0.18 * math.sin(phase * 2.001)
    right = math.sin(phase * 1.001) + 0.18 * math.sin(phase * 1.999)
    return amp * left / 1.18, amp * right / 1.18


def bell_tone(freq: float, local_time: float, duration: float) -> float:
    """Title旋律に使う透明感のあるベル風音色を合成する。"""
    amp = envelope(local_time, duration, 0.02, 0.18)
    amp *= math.exp(-2.4 * local_time / duration)
    phase = math.tau * freq * local_time
    return amp * (
        math.sin(phase)
        + 0.35 * math.sin(phase * 2.01)
        + 0.14 * math.sin(phase * 3.98)
    ) / 1.49


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


def render_title_sample(sample_index: int) -> tuple[int, int]:
    """Title用の静かで余白のあるステレオPCM値を生成する。"""
    time = sample_index / SAMPLE_RATE
    beat_position = time / TITLE_BEAT_SECONDS
    bar_index = min(TITLE_BARS - 1, int(beat_position // BEATS_PER_BAR))
    beat_in_bar = beat_position - bar_index * BEATS_PER_BAR
    bar_time = beat_in_bar * TITLE_BEAT_SECONDS
    bar_duration = BEATS_PER_BAR * TITLE_BEAT_SECONDS
    chord = TITLE_CHORDS[bar_index % len(TITLE_CHORDS)]

    left = 0.0
    right = 0.0
    for chord_note in chord:
        pad_left, pad_right = ambient_pad(frequency(chord_note), bar_time, bar_duration)
        left += 0.075 * pad_left
        right += 0.075 * pad_right

    half_bar_index = min(1, int(beat_in_bar // 2.0))
    half_bar_time = (beat_in_bar - half_bar_index * 2.0) * TITLE_BEAT_SECONDS
    melody_index = (bar_index * 2 + half_bar_index) % len(TITLE_MELODY)
    melody = bell_tone(
        frequency(TITLE_MELODY[melody_index]),
        half_bar_time,
        TITLE_BEAT_SECONDS * 2.0,
    )
    pan = -0.18 if half_bar_index == 0 else 0.18
    left += 0.16 * melody * (1.0 - pan)
    right += 0.16 * melody * (1.0 + pan)

    low_note = chord[0]
    low_phase = math.tau * frequency(low_note) / 2.0 * bar_time
    low_envelope = envelope(bar_time, bar_duration, 0.8, 0.25)
    left += 0.055 * low_envelope * math.sin(low_phase)
    right += 0.055 * low_envelope * math.sin(low_phase * 1.001)

    master_gain = 0.58
    left_pcm = int(max(-1.0, min(1.0, left * master_gain)) * 32_767)
    right_pcm = int(max(-1.0, min(1.0, right * master_gain)) * 32_767)
    return left_pcm, right_pcm


def write_track(
    output_path: Path,
    duration_seconds: float,
    renderer: Callable[[int], tuple[int, int]],
) -> int:
    """指定した合成処理からWAVを書き出し、ループ境界のPCM段差を返す。"""
    output_path.parent.mkdir(parents=True, exist_ok=True)
    total_samples = int(duration_seconds * SAMPLE_RATE)
    first_sample: tuple[int, int] | None = None
    last_sample: tuple[int, int] | None = None

    with wave.open(str(output_path), "wb") as output:
        output.setnchannels(CHANNELS)
        output.setsampwidth(2)
        output.setframerate(SAMPLE_RATE)
        buffer = bytearray()
        for sample_index in range(total_samples):
            sample = renderer(sample_index)
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

    return boundary_jump


def generate() -> None:
    """対照的なTitle曲とプレイ曲を生成し、各ループ境界を検証する。"""
    battle_boundary_jump = write_track(BATTLE_OUTPUT_PATH, DURATION_SECONDS, render_sample)
    title_boundary_jump = write_track(
        TITLE_OUTPUT_PATH,
        TITLE_DURATION_SECONDS,
        render_title_sample,
    )

    print(
        f"generated: {BATTLE_OUTPUT_PATH}\n"
        f"duration: {DURATION_SECONDS:.1f}s, boundary_jump: {battle_boundary_jump}\n"
        f"generated: {TITLE_OUTPUT_PATH}\n"
        f"duration: {TITLE_DURATION_SECONDS:.1f}s, boundary_jump: {title_boundary_jump}\n"
        f"sample_rate: {SAMPLE_RATE}Hz"
    )


if __name__ == "__main__":
    generate()
