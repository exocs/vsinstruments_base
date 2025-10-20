using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Server;
using Instruments.Core;
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
		//     Method delegate for callbacks that request a file.
		public delegate void RequestFileCallback(FileTree.Node file);
		//
		// Summary:
		//     A single file request that can be queued for the manager to process.
		protected class FileRequest
		{
			//
			// Summary:
			//     Unique identifier of this request.
			private int _id;
			//
			// Summary:
			//     Owning file manager.
			private FileManagerServer _owner;
			//
			// Summary:
			//     The player the file is requested from.
			private IServerPlayer _source;
			//
			// Summary:
			//     Relative file path to the requested file.
			private string _file;
			//
			// Summary:
			//     Completion callback.
			private RequestFileCallback _completionCallback;
			//
			// Summary:
			//     Creates new file request.
			public FileRequest(FileManagerServer owner, IServerPlayer source, int id, string file, RequestFileCallback callback)
			{
				_owner = owner;
				_source = source;
				_file = file;
				_id = id;
				_completionCallback = callback;
			}
			//
			// Summary:
			//     Returns the target data path of this request.
			public string DataPath
			{
				get
				{
					return GetDataPath(_source, _file);
				}
			}
			//
			// Summary:
			//     Completes this request.
			public void Complete(FileTree.Node file)
			{
				_completionCallback(file);
			}
		}
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
		//     Pending requests by their id.
		// TODO@exocs: Timeout and clear on player disconnection
		protected Dictionary<int, FileRequest> Requests { get; private set; }

		//
		// Summary:
		//     Last used request ID.
		private static int _requestID;
		//
		// Summary:
		//     Returns next request ID in sequence.
		private static int NextRequestID()
		{
			return _requestID++;
		}
		//
		// Summary:
		//     Creates new file manager.
		// Parameters:
		//   api: The game interface.
		//   localPath: Root directory of the user path.
		//   dataPath: Root directory of the data path.
		public FileManagerServer(ICoreServerAPI api, string localPath, string dataPath) :
			base(api, localPath, dataPath)
		{
			ServerAPI = api;
			ServerChannel = api.Network.RegisterChannel(Constants.Channel.FileManager)
				.RegisterMessageType<GetFileRequest>()
				.RegisterMessageType<GetFileResponse>()

				.SetMessageHandler<GetFileResponse>(OnGetFileResponse);

			Requests = new Dictionary<int, FileRequest>(32);
		}
		//
		// Summary:
		//     Creates new file manager.
		public FileManagerServer(ICoreServerAPI api, InstrumentModSettings settings) :
			this(api, settings.LocalSongsDirectory, settings.DataSongsDirectory)
		{
		}
		//
		// Summary:
		//     Requests the provided file from the source player. Fires callback on completion.
		//     This method will return locally cached files, if any are present before dispatching requests.
		public void GetFile(IServerPlayer source, string file, RequestFileCallback callback)
		{
			ServerAPI.Logger.Notification(
				$"Server fetching file:" +
				$"  PlayerUID: {source.PlayerUID}\n" +
				$"  ClientId: {source.ClientId}\n" +
				$"  File: {file}\n"
				);

			// If the file is already present, there is no need to create a request,
			// return the file directly instead:
			string dataPath = GetDataPath(source, file);
			FileTree.Node data = DataTree.Find(dataPath);
			if (data != null)
			{
				ServerAPI.Logger.Notification(
					$"Using data file:" +
					$"  PlayerUID: {source.PlayerUID}\n" +
					$"  ClientId: {source.ClientId}\n" +
					$"  File: {file}\n"
				);

				callback.Invoke(data);
				return;
			}

			// TODO@exocs: Validate the path and make sure it's not illicit!
			// For now at least something C:
			if (Path.IsPathFullyQualified(dataPath) || Path.IsPathRooted(dataPath))
				throw new InvalidDataException();


			// With the file not present, add the request to the "queue".
			int requestID = NextRequestID();
			FileRequest request = new FileRequest(this, source, requestID, file, callback);
			Requests.Add(requestID, request);

			// And send the actual request packet.
			GetFileRequest requestPacket = new GetFileRequest();
			requestPacket.File = file;
			requestPacket.RequestID = requestID;
			ServerChannel.SendPacket(requestPacket, source);
		}
		//
		// Summary:
		//     Requests the provided file from the source player. Fires callback on completion.
		//     This method will return locally cached files, if any are present before dispatching requests.
		protected void OnGetFileResponse(IServerPlayer source, GetFileResponse response)
		{
			int requestID = response.RequestID;
			if (Requests.TryGetValue(requestID, out FileRequest request))
			{
				FileTree.Node node;
				using (FileStream file = CreateFile(request.DataPath, out node))
				{
					Decompress(response.Data, file, response.Compression);
					file.Flush();
				}

				ServerAPI.Logger.Notification(
					$"Received remove file:" +
					$"  PlayerUID: {source.PlayerUID}\n" +
					$"  ClientId: {source.ClientId}\n" +
					$"  File: {request.DataPath}\n"
				);

				if (Requests.Remove(requestID))
				{
					request.Complete(node);
				}
			}
		}
		//
		// Summary:
		//     File received from the provided player.
		//protected void OnFileTransfer(IServerPlayer source, FileTransferPacket packet)
		void Foo()
		{
			// Add the file to the local collection?
			/*string relativePath = Path.Combine(SanitizeUID(source.PlayerUID), packet.Name);
			string fullPath = Path.Combine(InstrumentModSettings.Instance.DataSongsDirectory, relativePath);
			string directoryPath = Path.GetDirectoryName(fullPath);
			if (!Directory.Exists(directoryPath))
			{
				Directory.CreateDirectory(directoryPath);
			}

			using (FileStream fileStream = File.OpenWrite(fullPath))
			{
				Decompress(packet.Data, fileStream, packet.Compression);
			}

			// Find the transfer
			List<FileRequest> requests = FileRequests[source];
			int targetRequest = requests.FindIndex((request) =>
			{
				return string.Compare(request.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase) == 0;
			});

			FileRequest request = requests[targetRequest];
			requests.RemoveAt(targetRequest);

			// The file has been received, find it
			FileTree.Node received = DataTree.Find(relativePath);
			request.Receive(received);*/
		}
		//
		// Summary:
		//     Sends file to the provided player(s).
		public void SendFile(FileTree.Node node, params IServerPlayer[] destination)
		{
			/*if (node == null)
			{
				ServerAPI.Logger.Error("Requested invalid file!");
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
			}*/
		}
	}
}
