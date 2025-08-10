using System;
using System.Diagnostics;

using Elements.Core;

using FrooxEngine;

namespace MockVisemes;

public class RNGLipSyncComputer {

	public readonly OVRLipSyncInterface.Frame CurrentFrame = new();

	private enum SyllablePart {
		Consonant,
		Vowel,
	};

	private Viseme targetViseme;

	private OVRLipSyncInterface.Frame intermediateFrame;

	private SyllablePart currentSyllablePart;

	private readonly Stopwatch syllableStopwatch = new();

	private readonly Stopwatch frameStopwatch = new();

	private readonly Random random = Random.Shared;

	private float _smoothing;
	public float Smoothing {
		get => _smoothing;
		set {
			if (value != _smoothing) {
				_smoothing = value;
				UpdateLerpSpeed();
				if (value <= 0) {
					intermediateFrame = null;
				}
			}
		}
	}

	public float LerpSpeed { get; private set; }

	private void UpdateLerpSpeed() {
		if (Smoothing > 0) {
			float amplitude = MockVisemes.MaxLerpSpeed - MockVisemes.MinLerpSpeed;
			LerpSpeed = (1f - MathX.Clamp(Smoothing, 0, 1)) * amplitude + MockVisemes.MinLerpSpeed;
		} else {
			LerpSpeed = 0;
		}
	}

	public RNGLipSyncComputer() {
		ResetContext();
		MockVisemes.MinLerpSpeedKey.OnChanged += (_) => UpdateLerpSpeed();
		MockVisemes.MaxLerpSpeedKey.OnChanged += (_) => UpdateLerpSpeed();
	}

	public void ResetContext() {
		CurrentFrame.Reset();
		intermediateFrame = null;
		NextConsonant();
		syllableStopwatch.Start();
		frameStopwatch.Start();
	}

	public OVRLipSyncInterface.Frame ProcessFrame(float[] audioBuffer) {
		float volume = 0;
		foreach (float f in audioBuffer) {
			volume += MathX.Abs(f);
		}
		volume /= audioBuffer.Length;
		volume = MathX.FilterInvalid(volume);
		volume = MathX.Clamp01(volume);
		volume = MathX.Pow(volume, 0.5f);

		OVRLipSyncInterface.Frame targetFrame = new();
		targetFrame.Visemes[(int)targetViseme] = volume;

		if (LerpSpeed > 0) {
			if (intermediateFrame == null) {
				intermediateFrame = new();
				intermediateFrame.CopyInput(CurrentFrame);
			}

			for (int i = 0; i < OVRLipSyncInterface.VisemeCount; i++) {
				CurrentFrame.Visemes[i] = MathX.SmoothLerp(
					CurrentFrame.Visemes[i],
					targetFrame.Visemes[i],
					ref intermediateFrame.Visemes[i],
					(float)(frameStopwatch.Elapsed.TotalSeconds * LerpSpeed));
			}
		} else {
			targetFrame.Visemes.CopyTo(CurrentFrame.Visemes, 0);
		}
		CurrentFrame.frameNumber++;
		frameStopwatch.Restart();

		if (syllableStopwatch.ElapsedMilliseconds > SyllableTimeout(currentSyllablePart)) {
			if (currentSyllablePart == SyllablePart.Consonant) {
				NextVowel();
			} else {
				NextConsonant();
			}
			syllableStopwatch.Restart();
		}

		return CurrentFrame;
	}

	private int RandomConsonant => 1 + random.Next(9);

	private int RandomVowel => 10 + random.Next(5);

	private void NextConsonant() {
		targetViseme = (Viseme)RandomConsonant;
		currentSyllablePart = SyllablePart.Consonant;
	}

	private void NextVowel() {
		targetViseme = (Viseme)RandomVowel;
		currentSyllablePart = SyllablePart.Vowel;
	}

	private static long SyllableTimeout(SyllablePart syllablePart) {
		return syllablePart switch {
			SyllablePart.Consonant => 100,
			SyllablePart.Vowel => 200,
			_ => 0,
		};
	}
}
