using System.IO;
using Vintagestory.API.Client;
using Instruments.Network.Packets;
using Instruments.Core;

namespace Instruments.Files
{
	//
	// Summary:
	//     This class handles file transfers on the client side.
	public class FileManagerClient : FileManager
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
		//     Creates new file manager.
		// Parameters:
		//   api: The game interface.
		//   localPath: Root directory of the user path.
		//   dataPath: Root directory of the data path.
		public FileManagerClient(ICoreClientAPI api, string localPath, string dataPath) :
			base(api, localPath, dataPath)
		{
			ClientAPI = api;
			ClientChannel = api.Network.RegisterChannel(Constants.Channel.FileManager)
				.RegisterMessageType<GetFileRequest>()
				.RegisterMessageType<GetFileResponse>()

				.SetMessageHandler<GetFileRequest>(OnGetFileRequest);
		}
		//
		// Summary:
		//     Creates new file manager.
		public FileManagerClient(ICoreClientAPI api, InstrumentModSettings settings) :
			this(api, settings.LocalSongsDirectory, settings.DataSongsDirectory)
		{
		}
		//
		// Summary:
		//     Callback raised when the server requests a file.
		protected void OnGetFileRequest(GetFileRequest packet)
		{
			ClientAPI.ShowChatMessage(
				$"Server file request received:" +
				$"  ID: {packet.RequestID}\n" +
				$"  File: {packet.File}\n"
				);

			FileTree.Node node = UserTree.Find(packet.File);
			if (node == null)
			{
				// TODO@exocs:
				//  If the user moved or removed the file shortly after they sent a request..
				//  just scold them for being an idiot honestly. Fix this later.
				ClientAPI.ShowChatMessage("Why are you like this?");
				return;
			}

			GetFileResponse response = new GetFileResponse();
			response.RequestID = packet.RequestID;
			FileToPacket(node, response);
			ClientChannel.SendPacket(response);
		}
	}
}
