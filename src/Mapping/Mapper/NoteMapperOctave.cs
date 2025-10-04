using System;
using Midi;

namespace Instruments.Mapping.Mappers
{
	//
	// Summary:
	//     This object creates mapping that will use a single pitch sample
	//     from each provided octave to fill the rest of the octave.
	//
	public sealed class NoteMapperOctave<T> : NoteMappingBase<T>.NoteMapperBase
	{
#nullable enable
		private readonly T?[] _values;
		private readonly Pitch _pitch;

		public NoteMapperOctave(Pitch pitch)
		{
			// Pre-allocate space for all items, null entries
			// represent missing items that will be remapped
			// to existing entries with modulated pitch
			_values = new T?[Constants.Note.Count];
			_pitch = pitch;
		}

		public override bool Add(Pitch pitch, T value)
		{
			if (pitch.PositionInOctave() != _pitch.PositionInOctave())
				return false;

			int index = (int)pitch;
			if (_values[index] != null)
				return false;

			_values[index] = value;
			return true;
		}
		public override bool Map(NoteMappingBase<T> destination)
		{
			// First fill the entire destination using the distance mapper. This will
			// ensure all octaves, even with no samples are filled and useable.
			//
			// Certainly sub-optimal, but prevents code duplication. If proven not
			// performant enough, can be unwrapped and implemented as part of this.
			/*using (NoteMapperDistance<T> dist = new NoteMapperDistance<T>())
			{
				for (int p = 0; p < _values.Length; ++p)
				{
					T? value = _values[p];
					if (value != null)
						dist.Add((Pitch)p, value);
				}

				if (!dist.Map(destination))
					return false;
			}*/

			int? findSample(int start, int? end)
			{
				if (end == null) return null;

				for (int i = start; i < end; ++i)
				{
					if (_values[i] != null)
						return i;
				}

				return null;
			}
			void fillSample(int from, int? to, int? sample)
			{
				if (to == null || sample == null) return;

				T? item = _values[sample.Value];
				for (int i = from; i <= to; ++i)
					Set(destination, i, sample.Value, item);
			}
			int? findOctaveTop(int? bottom)
			{
				if (bottom == null) return null;

				int next = bottom.Value + Constants.Note.OctaveLength;
				if (next > Constants.Note.Count) return null;

				return next;
			}

			// Find the initial upper bound (first non-null item),
			// there is no lower bound in this situation (yet)
			int? hi = null;
			int o = 0;

			// Start filling the individual octaves by moving the lower and upper bounds
			// by a width of a single octave at all times. When a sample is found within
			// these bounds, use it for all pitches in the octave, otherwise just leave
			// it to the value that was already assigned prior (above).
			do
			{
				// Determine the index of the starting note in the
				// current octave
				int lo = o * Constants.Note.OctaveLength;

				// Find the lower and upper bounds of the octave
				hi = findOctaveTop(lo);

				// Find a sample within this octave
				int? s = findSample(lo, hi);
				// Fill the entire octave with the found sample,
				// if possible. The fill function handles it safely.
				fillSample(lo, hi, s);

				// Advance to the next octave index
				++o;
			}
			while (hi.HasValue);

			// All entries are assigned, map is complete.
			return true;

		}
		public override void Dispose()
		{
			Array.Clear(_values);
		}
	}
#nullable restore
}