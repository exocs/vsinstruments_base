using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Instruments.Network.Packets;

namespace Instruments.Files
{
	//
	// Summary:
	//     Determines the method of compression used.
	public enum CompressionMethod
	{
		None,
		Deflate
	}
	//
	// Summary:
	//     Base class for all file management operations, compressions and file transfers.
	public abstract class FileManager
	{
		//
		// Summary:	 
		//     The file tree that represents the user directory with local files.
		public FileTree UserTree { get; private set; }
		//
		// Summary:	 
		//     The file tree that represents the shared data directory with files received from the server or other clients.
		public FileTree DataTree { get; private set; }
		//
		// Summary:
		//     Compresses the provided stream using the Deflate algorithm.
		private static byte[] CompressDeflate(Stream source)
		{
			// Allocate enough memory up front to prevent unnecessary allocations. In vast majority of the cases
			// the compressed size should be lower than the initial size. There may be rare cases where e.g. the
			// input stream is already compressed and the size may end up larger - let the memory stream grow in
			// such cases, but don't account for them initially as these are unlikely.
			using (MemoryStream destination = new MemoryStream((int)source.Length))
			{
				// Run the source through the compression first and make sure the compression stream is closed,
				// to ensure it has been closed and disposed of properly before manipulating it further.
				using (DeflateStream compress = new DeflateStream(destination, CompressionMode.Compress, leaveOpen: true))
				{
					source.CopyTo(compress);
				}

				// Now that the compression is done, resize the output to the actual compressed size,
				// compating the resulting buffer in the process.
				destination.SetLength(destination.Position);
				destination.Seek(0, SeekOrigin.Begin);
				return destination.ToArray();
			}
		}
		//
		// Summary:
		//     Decompresses the provided stream using the Deflate algorithm.
		private static void DecompressDeflate(byte[] source, Stream destination)
		{
			MemoryStream sourceStream = new MemoryStream(source);
			using (DeflateStream decompressionStream = new DeflateStream(sourceStream, CompressionMode.Decompress, true))
			{
				// Run the source through the compression stream before resizing the output to the actual
				// size, dropping our initial conservative reserve and return to the start of the stream.
				decompressionStream.CopyTo(destination);
			}
		}
		//
		// Summary:
		//     Compresses the provided stream into a byte stream via the provided compression method.
		public static byte[] Compress(Stream source, CompressionMethod compression)
		{
			switch (compression)
			{
				case CompressionMethod.None:
					long size = (source.Length - source.Position);
					byte[] buffer = new byte[size];
					source.Read(buffer);
					return buffer;
				case CompressionMethod.Deflate:
					return CompressDeflate(source);
				default:
					throw new NotImplementedException();
			}
		}
		//
		// Summary:
		//     Decompresses the provided stream into a byte stream via the provided compression method.
		public static void Decompress(byte[] source, Stream destination, CompressionMethod compression)
		{
			switch (compression)
			{
				case CompressionMethod.None:
					destination.Write(source);
					break;
				case CompressionMethod.Deflate:
					DecompressDeflate(source, destination);
					break;
				default:
					throw new NotImplementedException();
			}
		}
		//
		// Summary:
		//     Creates new file manager.
		// Parameters:
		//   api: The game interface.
		//   localPath: Root directory of the user path.
		//   dataPath: Root directory of the data path.
		protected FileManager(ICoreAPI api, string userPath, string dataPath)
		{
			UserTree = new FileTree(userPath);
			DataTree = new FileTree(dataPath);
		}
		//
		// Summary:
		//     Creates file for writing at the provided location in the Data tree.
		protected FileStream CreateFile(string file, out FileTree.Node node)
		{
			string fullPath = Path.Combine(DataTree.Root.FullPath, file);
			string fullDirectoryPath = Path.GetDirectoryName(fullPath);
			if (!Directory.Exists(fullDirectoryPath))
			{
				Directory.CreateDirectory(fullDirectoryPath);
			}

			FileStream stream = new FileStream(fullPath, FileMode.CreateNew);
			node = DataTree.Find(file);
			return stream;
		}
		//
		// Summary:
		//     Fills the provided packet with the provided file data.
		protected void FileToPacket(FileTree.Node node, GetFileResponse packet, CompressionMethod compression = CompressionMethod.Deflate)
		{
			using (FileStream file = File.OpenRead(node.FullPath))
			{
				packet.Size = (int)(file.Length - file.Position);
				packet.Data = Compress(file, compression);
				packet.Compression = compression;
			}
		}
		//
		// Summary:
		//     Regex used for sanitization of file names.
		private static string _sanitizeFileNameRegex;
		//
		// Summary:
		//     Initialize static properties of the file manager.
		static FileManager()
		{
			char[] invalidCharacters = Path.GetInvalidFileNameChars()
				.Concat(Path.GetInvalidPathChars())
				.Distinct()
				.ToArray();

			_sanitizeFileNameRegex = $"[{Regex.Escape(new string(invalidCharacters))}]";
		}
		//
		// Summary:
		//     Returns sanitized unique user id from the provided user id.
		// Parameters:
		//   uid: Unique user id to sanitize.
		//   replacement: The string disallowed characters are replaced by.
		public static string SanitizeUID(string uuid, string replacement = "")
		{
			return Regex.Replace(uuid, _sanitizeFileNameRegex, replacement);
		}
		//
		// Summary:
		//     Returns the relative path for a file of provided player as a path in the data tree.
		public static string GetDataPath(IPlayer player, string file)
		{
			if (Path.IsPathFullyQualified(file))
				throw new ArgumentException("File must be relative path!");

			string uid = SanitizeUID(player.PlayerUID);
			if (!file.Contains(uid))
			{
				return Path.Combine(uid, file);
			}

			return file;
		}
	}
}
