using System;
using Midi;

namespace Instruments.Mapping
{
	//
	// Summary:
	//     Base of a container that associates value to their note pitch keys.
	public abstract class NoteMappingBase<T>
	{
		//
		// Summary:
		//     Contains single value associated to a pitch and stores its source pitch,
		//     i.e. the location it was mapped from if not mapped directly.
		protected struct Item
		{
			public T Value;
			public Pitch Source;

			public Item(T value, Pitch source)
			{
				Value = value;
				Source = source;
			}
		}

		private readonly Item[] entries;

		public NoteMappingBase()
		{
			entries = new Item[Constants.Note.Count];
		}

		protected void Set(Pitch pitch, Pitch samplePitch, T value)
		{
			entries[(int)pitch] = new Item(value, samplePitch);
		}

		protected void Clear()
		{
			Array.Clear(entries);
		}

		protected Item GetItem(Pitch pitch)
		{
			return entries[(int)pitch];
		}

		public T GetValue(Pitch pitch)
		{
			return entries[(int)pitch].Value;
		}

		//
		// Summary:
		//     Base for object responsible for building the map from provided cache of items.
		public abstract class NoteMapperBase : IDisposable
		{
			public abstract bool Add(Pitch pitch, T item);
			public abstract bool Map(NoteMappingBase<T> destination);
			public abstract void Dispose();

			protected static void Set(NoteMappingBase<T> destination, int valueIndex, int sampleIndex, T value)
			{
				Pitch current = (Pitch)valueIndex;
				Pitch sample = (Pitch)sampleIndex;
				destination.Set(current, sample, value);
			}
			protected static void Clear(NoteMappingBase<T> destination)
			{
				destination.Clear();
			}
		}
	}

	//
	// Summary:
	//     Base interface for objects that implement a note-object mapping relation.
	public static class NoteMappingUtility
	{
		//
		// Summary:
		//     Computes the pitch modulation necessary for the provided note pitch,
		//     considering its source pitch as reference.
		public static float ComputeRelativePitch(Pitch target, Pitch source)
		{
			// See Equal Temperament for reference,
			// in short each semitone = ×2^(1/12).

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

		//
		// Summary:
		//     Computes the pitch modulation necessary for the provided note pitch,
		//     considering its source pitch as reference. Pitch extension method.
		public static float RelativePitch(this Pitch source, Pitch target)
		{
			return ComputeRelativePitch(target, source);
		}
	}
}