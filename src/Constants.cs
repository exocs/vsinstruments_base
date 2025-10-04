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

		public struct Channel
		{
			public const string Note = "noteTest";

			public const string Abc = "abc";
		}
	}

	/*
	static class NoteExtensions
	{
		private static readonly float[] FrequencyTable = new float[]
		{
			8.175799f, 8.661957f, 9.177024f, 9.722718f, 10.30086f, 10.91338f, 11.56233f, 12.24986f, 12.97827f, 13.75000f, 14.56762f, 15.43385f,				//-1
			16.35160f, 17.32391f, 18.35405f, 19.44544f, 20.60172f, 21.82676f, 23.12465f, 24.49971f, 25.95654f, 27.50000f, 29.13524f, 30.86771f,				// 0
			32.70320f, 34.64783f, 36.70810f, 38.89087f, 41.20344f, 43.65353f, 46.24930f, 48.99943f, 51.91309f, 55.00000f, 58.27047f, 61.73541f,				// 1
			65.40639f, 69.29566f, 73.41619f, 77.78175f, 82.40689f, 87.30706f, 92.49861f, 97.99886f, 103.8262f, 110.0000f, 116.5409f, 123.4708f,				// 2
			130.8128f, 138.5913f, 146.8324f, 155.5635f, 164.8138f, 174.6141f, 184.9972f, 195.9977f, 207.6523f, 220.0000f, 233.0819f, 246.9417f,				// 3
			261.6256f, 277.1826f, 293.6648f, 311.1270f, 329.6276f, 349.2282f, 369.9944f, 391.9954f, 415.3047f, 440.0000f, 466.1638f, 493.8833f,				// 4
			523.2511f, 554.3653f, 587.3295f, 622.2540f, 659.2551f, 698.4565f, 739.9888f, 783.9909f, 830.6094f, 880.0000f, 932.3275f, 987.7666f,				// 5
			1046.502f, 1108.731f, 1174.659f, 1244.508f, 1318.510f, 1396.913f, 1479.978f, 1567.982f, 1661.219f, 1760.000f, 1864.655f, 1975.533f,				// 6
			2093.005f, 2217.461f, 2349.318f, 2489.016f, 2637.020f, 2793.826f, 2959.955f, 3135.963f, 3322.438f, 3520.000f, 3729.310f, 3951.066f,				// 7
			4186.009f, 4434.922f, 4698.636f, 4978.032f, 5274.041f, 5587.652f, 5919.911f, 6271.927f, 6644.875f, 7040.000f, 7458.620f, 7902.133f,				// 8
			8372.018f, 8869.844f, 9397.273f, 9956.063f, 10548.080f, 11175.300f, 11839.820f, 12543.850f, 13289.750f, 14080.000f, 14917.240f, 15804.270f,		// 9
			16744.040f, 17739.690f, 18794.550f, 19912.130f, 21096.160f, 22350.610f, 23679.640f, 25087.710f, 26579.500f, 28160.000f, 29834.480f, 31608.530f	// 10
		};

		public static float Frequency(this Pitch pitch)
		{
			return FrequencyTable[(int)pitch];
		}
	}
	*/
}