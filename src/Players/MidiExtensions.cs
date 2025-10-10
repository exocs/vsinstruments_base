using MidiParser;

namespace Instruments.Players
{
	//
	// Summary:
	//     Convenience class for MIDI file and player extensions.
	public static class MidiExtensions
	{
		//
		// Summary:
		//     Default, fallback beats per minute as specified by the standard.
		public const int DefaultBPM = 120;

		//
		// Summary:
		//     Finds the BPM meta events in any of the provided tracks.
		//     Fallbacks to default BPM of 120 as per the Midi standard.
		public static int ReadBPM(MidiTrack[] tracks, int defaultValue = DefaultBPM)
		{
			// The MIDI file should contain a track with the timing meta event,
			// that will contain the quarter notes per microseconds value, which
			// is converted into BPM in the MidiParser and used for playback timing.
			foreach (MidiTrack track in tracks)
			{
				foreach (MidiEvent midiEvent in track.MidiEvents)
				{
					if (midiEvent.Time > 0)
						break;

					if (midiEvent.MidiEventType == MidiEventType.MetaEvent &&
						midiEvent.MetaEventType == MetaEventType.Tempo)
					{
						return midiEvent.Arg2;
					}
				}
			}

			return defaultValue;
		}
		//
		// Summary:
		//     Finds the BPM meta events in any of the tracks of
		//     the provided MIDI file.
		public static int ReadBPM(this MidiFile file, int defaultValue = DefaultBPM)
		{
			if (file.TracksCount == 0)
				return defaultValue;

			return ReadBPM(file.Tracks, defaultValue);
		}
		//
		// Summary:
		//     Returns the duration of this track in ticks.
		public static int ReadTrackDurationInTicks(MidiTrack track)
		{
			// Find the last event and converts it tick time
			// to real-time duration, to know when this track ends.
			int count = track.MidiEvents.Count;
			if (count > 0)
			{
				return track.MidiEvents[count - 1].Time;
			}
			return 0;
		}
		//
		// Summary:
		//     Returns the duration of the specified track in seconds.
		public static double ReadTrackDuration(this MidiFile midi, int track)
		{
			MidiTrack midiTrack = midi.Tracks[track];
			int ticksDuration = ReadTrackDurationInTicks(midiTrack);
			if (ticksDuration == 0)
				return 0;

			int bpm = midi.ReadBPM();
			double durationSeconds = TicksToTime(ticksDuration, bpm, midi.TicksPerQuarterNote);
			return durationSeconds;
		}
		//
		// Summary:
		//     Returns the longest duration of this file in seconds.
		public static double ReadMaxTrackDuration(this MidiFile midi)
		{
			int maxTicks = 0;
			for (int i = 0; i < midi.TracksCount; ++i)
			{
				MidiTrack midiTrack = midi.Tracks[i];
				int ticksDuration = ReadTrackDurationInTicks(midiTrack);
				if (ticksDuration > maxTicks)
					maxTicks = ticksDuration;
			}

			if (maxTicks == 0)
				return 0;
			int bpm = midi.ReadBPM();
			double durationSeconds = TicksToTime(maxTicks, bpm, midi.TicksPerQuarterNote);
			return durationSeconds;
		}
		//
		// Summary:
		//     Converts elapsed time in seconds to elapsed ticks.
		//
		// Parameters:
		//   seconds: Time in (elapsed) seconds to convert to ticks.
		//   bpm: Track beats per minute.
		//   ticksPerQuaterNote: Track ticks per quarter note. (Defined in midi file)
		public static long TimeToTicks(double seconds, int bpm, int ticksPerQuarterNote)
		{
			double secondsPerQuarterNote = 60.0 / (double)bpm;
			return (long)(seconds * (ticksPerQuarterNote / secondsPerQuarterNote));
		}
		//
		// Summary:
		//     Converts elapsed ticks to elapsed time in seconds.
		//
		// Parameters:
		//   ticks: Time in (elapsed) ticks to convert to seconds.
		//   bpm: Track beats per minute.
		//   ticksPerQuaterNote: Track ticks per quarter note. (Defined in midi file)
		public static double TicksToTime(long ticks, int bpm, int ticksPerQuaterNote)
		{
			double secondsPerQuarterNote = 60.0 / (double)bpm;
			return ticks * (secondsPerQuarterNote / ticksPerQuaterNote);
		}
	}
}
