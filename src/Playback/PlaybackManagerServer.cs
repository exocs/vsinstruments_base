using Vintagestory.API.Server;
using Instruments.Files;
using Instruments.Network.Packets;

namespace Instruments.Playback
{
	public class PlaybackManagerServer : PlaybackManager
	{
		//
		// Summary:
		//     Returns the interface to the game.
		protected ICoreServerAPI ServerAPI { get; }
		//	 
		// Summary:	 
		//     Returns the networking channel for file transactions.
		protected IServerNetworkChannel ServerChannel { get; private set; }
		//
		// Summary:	 
		//     Server-side file manager used to dispatch file requests.
		protected FileManagerServer ServerFileManager { get; private set; }
		//
		// Summary:
		//     Creates new server side playback manager.
		public PlaybackManagerServer(ICoreServerAPI api, FileManagerServer fileManager)
			: base(api, fileManager)
		{
			ServerAPI = api;
			ServerChannel = api.Network.RegisterChannel(Constants.Channel.Playback)
				.RegisterMessageType<StartPlaybackRequest>()
				.RegisterMessageType<StartPlaybackBroadcast>()
				.RegisterMessageType<StartPlaybackOwner>()

				.SetMessageHandler<StartPlaybackRequest>(OnStartPlaybackRequest);

			ServerFileManager = fileManager;
		}
		//
		// Summary:
		//     Called when a client requests playback start.
		protected void OnStartPlaybackRequest(IServerPlayer source, StartPlaybackRequest packet)
		{
			// Validate whether the request was valid whatsoever, check for malicious attempts,
			// deny anything illicit, bad or broken:
			if (!ValidatePlaybackRequest(source, packet))
			{
				// TODO@exocs: Handle?
				return;
			}

			// Request the specified file:
			ServerFileManager.RequestFile(source, packet.File, (node, context) =>
			{
				// Upon receiving the request file, approve the request and
				// start the playback both locally and for all relevant clients:
				StartPlayback(source, packet.File, node, packet.Channel, packet.Instrument);
			});
		}
		//
		// Summary:
		//     Called when a client requests playback start.
		protected void StartPlayback(IServerPlayer source, string sourceFile, FileTree.Node serverFile, int channel, int instrumentType)
		{
			// Send a packet to all the clients except for the actual instigator, as all these players
			// will use the "shared" data path with the source player UID stamped in the path.
			StartPlaybackBroadcast broadcast = new StartPlaybackBroadcast();
			broadcast.ClientId = source.ClientId;
			broadcast.Channel = channel;
			broadcast.File = serverFile.RelativePath;
			broadcast.Instrument = instrumentType;
			ServerChannel.BroadcastPacket(broadcast, exceptPlayers: source);

			// Send a packet to the actual instigator, as they will be playing local file:
			StartPlaybackOwner owner = new StartPlaybackOwner();
			owner.Channel = channel;
			owner.File = sourceFile;
			owner.Instrument = instrumentType;
			ServerChannel.SendPacket(owner, source);
		}
		//
		// Summary:
		//     Returns whether specified player can start playback with provided data.
		protected bool ValidatePlaybackRequest(IServerPlayer source, StartPlaybackRequest packet)
		{
			return true;
		}
		//
		// Summary:
		//     Updates the playback manager.
		public override void Update(float deltaTime)
		{
		}
	}
}
