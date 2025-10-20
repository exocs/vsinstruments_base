using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Instruments.Files;
using Instruments.Network.Packets;
using Instruments.Types;
using Instruments.Players;

namespace Instruments.Playback
{
	public class PlaybackManagerClient : PlaybackManager
	{
		//	 
		// Summary:	 
		//     Returns the interface to the game.
		protected ICoreClientAPI ClientAPI { get; }
		//	 
		// Summary:	 
		//     Returns the networking channel for file transactions.
		protected IClientNetworkChannel ClientChannel { get; private set; }
		//
		// Summary:	 
		//     Client-side file manager used to fetch files.
		protected FileManagerClient ClientFileManager { get; private set; }
		//
		// Summary:
		//     Music players per player.
		protected Dictionary<int, MusicPlayerMidi> ClientPlayers { get; private set; }
		//
		// Summary:
		//     Creates new client side playback manager.
		public PlaybackManagerClient(ICoreClientAPI api, FileManagerClient fileManager)
			: base(api, fileManager)
		{
			ClientAPI = api;
			ClientChannel = api.Network.RegisterChannel(Constants.Channel.Playback)
				.RegisterMessageType<StartPlaybackRequest>()
				.RegisterMessageType<StartPlaybackBroadcast>()
				.RegisterMessageType<StartPlaybackOwner>()

				.SetMessageHandler<StartPlaybackBroadcast>(OnStartPlaybackBroadcast)
				.SetMessageHandler<StartPlaybackOwner>(OnStartPlaybackOwner);

			ClientFileManager = fileManager;
			ClientPlayers = new Dictionary<int, MusicPlayerMidi>(64);
		}
		//
		// Summary:
		//     Asks the server to start the playback with provided data.
		public void RequestStartPlayback(string file, int channel, InstrumentType instrumentType)
		{
			StartPlaybackRequest request = new StartPlaybackRequest();
			request.File = file;
			request.Channel = channel;
			request.Instrument = instrumentType.ID;
			ClientChannel.SendPacket(request);
		}
		//
		// Summary:
		//     Callback raised when playback starts.
		//     This callback is called for all players except the actual instigator (the instrument player).
		protected void OnStartPlaybackBroadcast(StartPlaybackBroadcast packet)
		{
			long elapsedMilliseconds = ClientAPI.World.ElapsedMilliseconds;
			IPlayer player = ClientAPI.World.AllOnlinePlayers[packet.ClientId];
			ClientFileManager.RequestFile(player, packet.File, (node, context) =>
			{
				long startTimeMsec = (long)context;
				CreateMusicPlayer(player, node, packet.Channel, InstrumentType.Find(packet.Instrument), startTimeMsec);

			}, elapsedMilliseconds);
		}
		//
		// Summary:
		//     Callback raised when playback starts.
		//     This callback is called for the actual instigator only.
		protected void OnStartPlaybackOwner(StartPlaybackOwner packet)
		{
			// TODO@exocs: Play the file!
			FileTree.Node node = ClientFileManager.UserTree.Find(packet.File);
			CreateMusicPlayer(ClientAPI.World.Player, node, packet.Channel, InstrumentType.Find(packet.Instrument), ClientAPI.World.ElapsedMilliseconds);
		}
		//
		// Summary:
		//     Creates music player for the provided player.
		//     If a player was present previously, it is replaced.
		protected void CreateMusicPlayer(IPlayer player, FileTree.Node node, int channel, InstrumentType instrumentType, long startTimeMsec = 0)
		{
			int clientId = player.ClientId;
			if (ClientPlayers.Remove(clientId, out MusicPlayerMidi previousPlayer))
			{
				if (previousPlayer.IsPlaying)
					previousPlayer.Stop();

				previousPlayer.Dispose();
			}

			try
			{
				MidiParser.MidiFile midi = new MidiParser.MidiFile(node.FullPath);
				MusicPlayerMidi musicPlayer = new PlayerMusicPlayerMidi(ClientAPI, player, instrumentType);
				musicPlayer.Play(midi, channel);

				double time = (ClientAPI.World.ElapsedMilliseconds - startTimeMsec) / 1000.0;
				double duration = musicPlayer.Duration;
				musicPlayer.Seek(Math.Min(time, duration));

				ClientPlayers.Add(clientId, musicPlayer);
			}
			catch
			{
				// Bad.
			}
		}
		//
		// Summary:
		//     Updates this managed and all its music players.
		public override void Update(float deltaTime)
		{
			// TODO@exocs: Not very efficient.
			var musicPlayers = ClientPlayers.Values;
			foreach (MusicPlayerMidi player in musicPlayers)
			{
				if (player.IsPlaying) player.Update(deltaTime);
			}
		}
	}
}
