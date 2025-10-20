using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Midi;
using Instruments.Types;

namespace Instruments.Players
{
	//
	// Summary:
	//     TODO@exocs: Unify with base, cleanup, refactor.
	//     Music player that allows playing Midi songs locally.
	class PlayerMusicPlayerMidi : MusicPlayerMidi, IDisposable
	{
		//
		// Summary:
		//     List of all active sounds per key.
		private ILoadedSound[] _activeSounds;
		//
		// Summary:
		//     The player this player belongs to.
		private IPlayer _player;
		//
		// Summary:
		//     Creates new preview music player that plays all the notes locally.
		public PlayerMusicPlayerMidi(ICoreAPI api, IPlayer player, InstrumentType instrumentType)
			: base(api, instrumentType)
		{
			_activeSounds = new ILoadedSound[Constants.Note.NoteCount];
			_player = player;
		}
		//
		// Summary:
		//     Starts playing a note.
		protected override void OnNoteOn(Pitch pitch, float velocity, int channel, float time)
		{
			if (CoreAPI is not ICoreClientAPI clientAPI)
			{
				return;
			}

			Entity playerEntity = _player.Entity;
			if (playerEntity == null)
				return;

			InstrumentType.GetPitchSound(pitch, out string assetPath, out float modPitch);
			SoundParams soundParams = new SoundParams(new AssetLocation("instruments", assetPath));
			soundParams.Volume = Constants.Playback.GetVolumeFromVelocity(velocity);
			soundParams.DisposeOnFinish = true;
			soundParams.RelativePosition = false;
			soundParams.Position = playerEntity.Pos.XYZFloat;
			soundParams.Pitch = modPitch;

			ILoadedSound sound = clientAPI.World.LoadSound(soundParams);
			if (sound != null)
			{
				int index = (int)pitch;

				// If, by any chance, the events are mismatched or something else has happened
				// and there is a sound already playing in the slot, simply stop it and replace
				// it immediately with the new sound.
				TryRemoveSound(index, Constants.Playback.MinFadeOutDuration);

				_activeSounds[index] = sound;
				sound.Start();
			}
		}
		//
		// Summary:
		//     Stops playing a note.
		protected override void OnNoteOff(Pitch pitch, float velocity, int channel, float time)
		{
			if (CoreAPI is not ICoreClientAPI)
				return;

			int index = (int)pitch;
			float fadeDuration = Constants.Playback.GetFadeDurationFromVelocity(velocity);
			TryRemoveSound(index, fadeDuration);
		}
		//
		// Summary:
		//     Removes a sound from the active sounds.
		// Parameters:
		//   fadeDuration: Duration to fade out the sound in (in seconds) or 0 for immediate.
		private void TryRemoveSound(int index, float fadeDuration)
		{
			ILoadedSound sound = _activeSounds[index];
			if (sound == null)
				return;

			if (fadeDuration <= 0)
			{
				sound.Dispose();
				_activeSounds[index] = null;
				return;
			}
			else
			{
				sound.FadeOutAndStop(fadeDuration);
				_activeSounds[index] = null;
				return;
			}
		}
		//
		// Summary:
		//     Callback raised when the playback stops.
		protected override void OnStop()
		{
			StopAllSounds(Constants.Playback.MinFadeOutDuration);
			base.OnStop();
		}
		//
		// Summary:
		//     Dispose of this player and all of its allocated resources and sounds.
		public override void Dispose()
		{
			StopAllSounds(0);
		}

		protected void StopAllSounds(float fadeDuration)
		{
			for (int i = 0; i < _activeSounds.Length; ++i)
				TryRemoveSound(i, fadeDuration);

			Array.Clear(_activeSounds);
		}
	}
}