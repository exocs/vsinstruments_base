using System;
using System.IO;
using System.IO.Compression;
using Vintagestory.API.Common;

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
		//     Returns the file tree this class operates in.
		public FileTree Tree { get; private set; }
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
		//   root: Root directory this manager will operate in.
		protected FileManager(ICoreAPI api, string root)
		{
			Tree = new FileTree(root);
		}
	}
}
