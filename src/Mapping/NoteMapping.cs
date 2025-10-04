using System;
using Midi;

namespace Instruments.Mapping
{
	//
	// Summary:
	//     Container that associates values and relative sound pitch value 
	//     Base of a container that associates value to their note pitch keys.
	//
	public class NoteMapping<T> : NoteMappingBase<T>
	{
		public NoteMapping()
			: base()
		{
		}

		//
		// Summary:
		//     Computes the pitch modulation necessary for the provided note pitch,
		//     considering its source pitch as reference.
		//
		public float GetRelativePitch(Pitch target)
		{
			// See Equal Temperament for reference,
			// in short each semitone = ×2^(1/12).
			Pitch source = GetItem(target).Source;

			// Semitone difference within the octave relative to source A
			int semitonesAboveInOctave = ((int)target % Constants.Note.OctaveLength) - ((int)source % Constants.Note.OctaveLength);
			if (semitonesAboveInOctave < 0)
			{
				semitonesAboveInOctave += 12; // wrap around octave
			}

			// Base multiplier within the octave
			float ratio = MathF.Pow(2f, semitonesAboveInOctave / (float)Constants.Note.OctaveLength);

			// Octave difference
			int octaveDiff = ((int)target / Constants.Note.OctaveLength) - ((int)source / Constants.Note.OctaveLength);
			ratio *= MathF.Pow(2f, octaveDiff);

			return ratio;
		}

		protected static float ComputePitch(Pitch source, Pitch target)
		{
			float ratio = target.Frequency() / source.Frequency();
			return ratio;
		}
	}
}