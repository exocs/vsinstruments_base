using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Midi;
using MidiParser;
using Instruments.Types;

namespace Instruments.Players
{
	//
	// Summary:
	//     Allows opening and playing back a MIDI file.
	public class MusicPlayerMidi
	{
		private ICoreAPI _coreAPI;
		private InstrumentType _instrumentType;

		private MidiTrack _midiTrack;

		private int _beatsPerMinute;
		private int _ticksPerQuarterNote;
		private int _ticksDuration;

		private double _elapsedTime;
		private double _duration;

		private int _eventIndex;
		private int _channel;

		//
		// Summary:
		//     Returns the duration of the played track in seconds.
		public double Duration
		{
			get
			{
				return _duration;
			}
		}
		//
		// Summary:
		//     Returns the elapsed time of this player in seconds.
		public double ElapsedTime
		{
			get
			{
				return _elapsedTime;
			}
		}
		//
		// Summary:
		//     Returns whether this player is currently playing.
		public bool IsPlaying
		{
			get
			{
				return _midiTrack != null && _elapsedTime < _duration;
			}
		}
		//
		// Summary:
		//     Returns whether this player has finished its playback.
		public bool IsFinished
		{
			get
			{
				return _midiTrack != null && _elapsedTime >= _duration;
			}
		}
		//
		// Summary:
		//     Returns the duration of this track in ticks.
		private int GetDuration(MidiTrack track)
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
		//     Converts elapsed time in seconds to elapsed ticks.
		//
		// Parameters:
		//   time: Time in (elapsed) seconds to convert to ticks.
		private long TimeToTicks(double seconds)
		{
			return MidiExtensions.TimeToTicks(seconds, _beatsPerMinute, _ticksPerQuarterNote);
		}
		//
		// Summary:
		//     Converts elapsed ticks to elapsed time in seconds.
		//
		// Parameters:
		//   ticks: Ticks to convert to (elapsed) time in seconds.
		private double TicksToTime(long ticks)
		{
			return MidiExtensions.TicksToTime(ticks, _beatsPerMinute, _ticksPerQuarterNote);
		}
		//
		// Summary:
		//     Plays the specified track of the provided MIDI file.
		public void Play(MidiFile midi, int channel)
		{
			if (IsPlaying)
			{
				throw new InvalidOperationException("Cannot start MIDI playback, the player is already playing!");
			}

			if (midi == null)
			{
				throw new InvalidOperationException("Cannot start MIDI playback, the provided file is invalid!");
			}

			_midiTrack = midi.Tracks[channel];

			_beatsPerMinute = midi.ReadBPM();
			_ticksPerQuarterNote = midi.TicksPerQuarterNote;

			_elapsedTime = 0;
			_ticksDuration = GetDuration(midi.Tracks[channel]);
			_duration = TicksToTime(_ticksDuration);

			_channel = channel;
			_eventIndex = 0;
		}
		//
		// Summary:
		//     Opens and plays the MIDI file at the provided file path.
		public void Play(string midiFilePath, int channel)
		{
			MidiFile midiFile = new MidiFile(midiFilePath);
			Play(midiFile, channel);
		}
		//
		// Summary:
		//     Update the playback of this player.
		//
		// Parameters:
		//   deltaTime: Elapsed time in seconds.
		public void Update(float deltaTime)
		{
			if (!IsPlaying)
			{
				throw new InvalidOperationException("Player is not playing!");
			}

			_elapsedTime += deltaTime;
			long elapsedTicks = TimeToTicks(_elapsedTime);

			// Clamp to bounds - the playback is finished.
			if (elapsedTicks > _ticksDuration)
				_elapsedTime = _duration;

			// Peeks one message in the track event list and if it
			// is time for it to be played, true is returned.
			bool tryPollEvent(out MidiEvent outEvent)
			{
				// Out of events range, no work left.
				if (_eventIndex >= _midiTrack.MidiEvents.Count)
				{
					outEvent = default;
					return false;
				}

				MidiEvent polledEvent = _midiTrack.MidiEvents[_eventIndex];
				if (polledEvent.Time <= elapsedTicks)
				{
					outEvent = polledEvent;
					++_eventIndex;
					return true;
				}

				outEvent = default;
				return false;
			}

			// Try polling for possible events to play
			while (tryPollEvent(out MidiEvent midiEvent))
			{

				if (midiEvent.MidiEventType == MidiEventType.NoteOn)
				{
					Pitch pitch = (Pitch)midiEvent.Note;
					int channel = (int)_channel;
					float time = (float)TicksToTime(midiEvent.Time);
					PlayNote(pitch, channel, time);
				}

				// TODO@exocs: Process other events
				// TODO@exocs: Add Seek() that can skip polling events
			}
		}

		public void Stop()
		{
			if (!IsPlaying)
			{
				throw new InvalidOperationException("Cannot stop MIDI playback, the player is not playing!");
			}

			_midiTrack = null;

			_beatsPerMinute = MidiExtensions.DefaultBPM;
			_ticksPerQuarterNote = 0;

			_elapsedTime = 0;
			_ticksDuration = 0;
			_duration = 0;

			_eventIndex = 0;
			_channel = 0;
		}

		protected virtual void PlayNote(Pitch pitch, int channel, float time)
		{
			PlaySound(pitch, channel, time);
		}

		public MusicPlayerMidi(ICoreAPI api, InstrumentType instrumentType)
		{
			_coreAPI = api;
			_instrumentType = instrumentType;
		}

		protected void PlaySound(Pitch pitch, int channel, float time)
		{
			// Does it actually have the wrong tuning after all?

			_instrumentType.GetPitchSound(pitch, out string assetPath, out float modPitch);

			if (_coreAPI is ICoreClientAPI clientAPI)
			{
				SoundParams soundParams = new SoundParams(new AssetLocation("instruments", assetPath));
				soundParams.Volume = 1.0f;
				soundParams.DisposeOnFinish = true;
				soundParams.RelativePosition = false;
				soundParams.Position = clientAPI.World.Player.Entity.Pos.XYZFloat;
				soundParams.Pitch = modPitch;
				ILoadedSound sound = clientAPI.World.LoadSound(soundParams);
				if (sound != null)
				{
					sound.Start();
					sound.FadeOutAndStop(1.0f/*+note.Length.Seconds*/);
				}
			}
		}

	}
}