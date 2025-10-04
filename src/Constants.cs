using System;
using Midi;

namespace Instruments
{
	public static class Constants
	{
		public struct Math
		{
			public const float PI = MathF.PI;
		}

		public struct Note
		{
			public const int Count = (int)Pitch.G9;
			public const int OctaveLength = (int)Pitch.C0 - (int)Pitch.CNeg1;
			public const int OctaveCount = Count / OctaveLength;
		}

		public struct Packet
		{
			// TODO@exocs: Figure out what these are and rename. :)
			public const int NameChangeID = 1004;
			public const int BandChangeID = 1005;
			public const int SongSelectID = 1006;
		}
	}

	static class NoteExtensions
	{
		private static readonly float[] FrequencyTable = new float[]
		{
			8.1758f, 8.66196f, 9.17702f, 9.72272f, 10.3009f, 10.9134f, 11.5623f, 12.2499f, 12.9783f, 13.75f, 14.5676f, 15.4339f,	// -1
			16.3516f, 17.3239f, 18.3540f, 19.4454f, 20.6017f, 21.8268f, 23.1247f, 24.4997f, 25.9565f, 27.5f, 29.1352f, 30.8677f,	// 0
			32.7032f, 34.6478f, 36.7081f, 38.8909f, 41.2034f, 43.6535f, 46.2493f, 48.9994f, 51.9131f, 55f, 58.2705f, 61.7354f,		// 1
			65.4064f, 69.2957f, 73.4162f, 77.7817f, 82.4069f, 87.3071f, 92.4986f, 97.9989f, 103.826f, 110f, 116.541f, 123.471f,		// 2
			130.813f, 138.591f, 146.832f, 155.563f, 164.814f, 174.614f, 184.997f, 195.998f, 207.652f, 220f, 233.082f, 246.942f,		// 3
			261.626f, 277.183f, 293.665f, 311.127f, 329.628f, 349.228f, 369.994f, 391.995f, 415.305f, 440f, 466.164f, 493.883f,		// 4
			523.251f, 554.365f, 587.330f, 622.254f, 659.255f, 698.456f, 739.989f, 783.991f, 830.609f, 880f, 932.328f, 987.767f,		// 5
			1046.50f, 1108.73f, 1174.66f, 1244.51f, 1318.51f, 1396.91f, 1479.98f, 1567.98f, 1661.22f, 1760f, 1864.66f, 1975.53f,	// 6
			2093f, 2217.46f, 2349.32f, 2489.02f, 2637.02f, 2793.83f, 2959.96f, 3135.96f, 3322.44f, 3520f, 3729.31f, 3951.07f,		// 7
			4186.01f, 4434.92f, 4698.64f, 4978.03f, 5274.04f, 5587.65f, 5919.91f, 6271.93f, 6644.88f, 7040f, 7458.62f, 7902.13f,	// 8
			8372.02f, 8869.84f, 9397.27f, 9956.06f, 10548.1f, 11175.3f, 11839.8f, 12543.9f											// 9
		};

		public static float Frequency(this Pitch pitch)
		{
			return FrequencyTable[(int)pitch];
		}
	}
}