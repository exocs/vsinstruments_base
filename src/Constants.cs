using System;
using Midi;

namespace Instruments
{
	//
	// Summary:
	//     This class contains variety of constants divided by their category.
	public static class Constants
	{
		//
		// Summary:
		//     This structure contains all mathematical constants.
		public struct Math
		{
			public const float PI = MathF.PI;
		}
		//
		// Summary:
		//     This structure contains all constants related to midi notes.
		public struct Note
		{
			//
			// Summary:
			//     The amount of notes including all octaves and accidentals.
			public const int NoteCount = (int)Pitch.G9;
			//
			// Summary:
			//     The length of a single octave in notes.
			public const int OctaveLength = (int)Pitch.C0 - (int)Pitch.CNeg1;
			//
			// Summary:
			//     The total number of octaves available.
			public const int OctaveCount = NoteCount / OctaveLength;
		}
		//
		// Summary:
		//     This structure contains constants related to networking packets.
		public struct Packet
		{
			public const int NameChangeID = 1004;
			public const int BandChangeID = 1005;
			public const int SongSelectID = 1006;

			//
			// Summary:
			//     Packet sent when a player tries to 'open' a music block. (TODO@exocs: Verify this assertion.)
			public const int MusicBlockOpenID = 69;
		}
		//
		// Summary:
		//     This structure contains constants related to networking channels.
		public struct Channel
		{
			public const string Note = "noteTest";

			public const string Abc = "abc";
		}
		//
		// Summary:
		//     This structure contains constants related to item and block attributes.
		public struct Attributes
		{
			//
			// Summary:
			//     Attribute of this type will contain the current mode the tool is set to.
			public const string ToolMode = "toolMode";
		}
	}
}