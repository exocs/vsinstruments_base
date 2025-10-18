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
				.RegisterMessageType<FileTransferPacket>()
				.SetMessageHandler<FileTransferPacket>(OnTransferFilePacket);
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
		//     Callback raised when the file manager receives a file.
		protected virtual void OnTransferFilePacket(FileTransferPacket packet)
		{
			ClientAPI.ShowChatMessage(
				$"Received file: {packet.Name}\n" +
				$"  Original size: {packet.Size}b\n" +
				$"  Compressed size: {packet.Data.Length}b\n"
				);

			using (MemoryStream decompressedFile = new MemoryStream())
			{
				Decompress(packet.Data, decompressedFile, packet.Compression);
				try
				{
					decompressedFile.Seek(0, SeekOrigin.Begin);
					MidiParser.MidiFile midi = new MidiParser.MidiFile(decompressedFile);
				}
				catch
				{

				}
			}
		}
	}
}
