using System.IO;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Instruments.Network.Packets;

namespace Instruments.Files
{
	//
	// Summary:
	//     This class handles file transfers on the server side.
	public class FileManagerServer : FileManager
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
		//     Creates new file manager.
		// Parameters:
		//   api: The game interface.
		//   root: Root directory this manager will operate in.
		public FileManagerServer(ICoreServerAPI api, string root) :
			base(api, root)
		{
			ServerAPI = api;
			ServerChannel = api.Network.RegisterChannel(Constants.Channel.FileManager)
				.RegisterMessageType<FileTransferPacket>();



			// For testing purposes
#if DEBUG
			api.RegisterCommand("fetchmidi", "Request a midi from the server.", "fetchmidi [path]", (player, groupId, args) =>
			{
				if (args.Length == 0)
				{
					player.SendMessage(groupId, "No arguments provided!", Vintagestory.API.Common.EnumChatType.Notification);
					return;
				}

				string path = args.PopAll();
				SendFile(path, player);
			}, "chat");
#endif
		}
		//
		// Summary:
		//     Sends file to the provided player(s).
		protected void SendFile(string path, params IServerPlayer[] destination)
		{
			FileTree.Node node = Tree.Find(path);
			if (node == null)
			{
				foreach (IServerPlayer player in destination)
					player.SendMessage(GlobalConstants.GeneralChatGroup, $"File {path} does not exist on the server.", Vintagestory.API.Common.EnumChatType.Notification);

				return;
			}

			using (FileStream file = File.OpenRead(node.FullPath))
			{
				FileTransferPacket packet = new FileTransferPacket();
				packet.Name = node.RelativePath;
				packet.Compression = CompressionMethod.Deflate;
				packet.Size = file.Length - file.Position;
				packet.Data = Compress(file, packet.Compression);
				ServerChannel.SendPacket(packet, destination);
			}
		}
	}
}
