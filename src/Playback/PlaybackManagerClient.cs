using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Instruments.Files;
using Instruments.Network.Packets;
using Instruments.Types;

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
			ClientAPI.ShowChatMessage(
				$"Start playback broadcast received:" +
				$"  Client: {packet.ClientId}\n" +
				$"  File: {packet.File}\n" +
				$"  Channel: {packet.Channel}\n" +
				$"  Instrument: {packet.Instrument}\n"
				);

			IPlayer player = ClientAPI.World.AllOnlinePlayers[packet.ClientId];
			ClientFileManager.RequestFile(player, packet.File, (node, context) =>
			{
				// TODO@exocs:
				//   Play the file or seek to the playback.
			});
		}
		//
		// Summary:
		//     Callback raised when playback starts.
		//     This callback is called for the actual instigator only.
		protected void OnStartPlaybackOwner(StartPlaybackOwner packet)
		{
			ClientAPI.ShowChatMessage(
				$"Start playback owner received:" +
				$"  File: {packet.File}\n" +
				$"  Channel: {packet.Channel}\n" +
				$"  Instrument: {packet.Instrument}\n"
				);

			// TODO@exocs: Play the file!
			FileTree.Node node = ClientFileManager.UserTree.Find(packet.File);
		}
	}
}
