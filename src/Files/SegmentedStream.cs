using System;
using System.IO;
using System.IO.Compression;

namespace Instruments.Files
{
	//
	// Summary:
	//     This object allows the user to compress and segment an input stream into multiple
	//     segment parts that can be later decompressed and reconstructed.
	public class SegmentedStream
	{
		//
		// Summary:
		//     Defines the types of supported compression types.
		public enum Compression
		{
			None,
			GZCompression,
		}
		//
		// Summary:
		//     This structure describes the original properties of the stream.
		public struct Header
		{
			//
			// Summary:
			//     The original size (prior to compression) in bytes.
			public long Size;
			//
			// Summary:
			//     The number of segments the source is divided into.
			public int SegmentsCount;
			//
			// Summary:
			//     Determines the type of compression the source underwent prior to segmentation.
			public Compression Compression;
			//
			// Summary:
			//     Creates new segmented stream header.
			// Parameters:
			//   size: The original (uncompressed) size in bytes.
			//   segments: The number of segments the source is divided into.
			//   compression: The type of compression used on the source.
			public Header(long size, int segments, Compression compression)
			{
				Size = size;
				SegmentsCount = segments;
				Compression = compression;
			}
		}
		//
		// Summary:
		//     This structure describes a single deconstructed segment.
		public struct Segment
		{
			//
			// Summary:
			//     This value represents the order of the segment.
			public int Index;
			//
			// Summary:
			//     The computed checksum of the source data.
			//public long Checksum;
			//
			// Summary:
			//     The segment data. Never larger in size than MaximumSegmentSizeInBytes.
			public byte[] Data;
			//
			// Summary:
			//     Creates new segment.
			// Parameters:
			//   index: The order of this segment.
			//   data: Source data to create this segment from.
			//   offset: The offset in the data buffer.
			//   count: The number of bytes to copy from the data buffer.
			public Segment(int index, byte[] data, int offset, int count)
			{
				Index = index;
				Data = new byte[count];
				Array.Copy(data, offset, Data, 0, count);
				//Checksum = ComputeChecksum(data);
			}
			//
			// Summary:
			//     Computes checksum for the provided byte buffer.
			/*public static long ComputeChecksum(Span<byte> buffer)
			{
				long checksum = 0;

				// Start by computing the checksum in chunks of 8 bytes,
				int i;
				for (i = 0; i + 8 <= buffer.Length; i += 8)
				{
					checksum += BitConverter.ToInt64(buffer.Slice(i, 8));
				}

				// And only then handle the remaining bytes.
				for (; i < buffer.Length; i++)
				{
					checksum += buffer[i];
				}

				return checksum;
			}*/
		}
		//
		// Summary:
		//     The header describing the original stream properties.
		private Header _header;
		//
		// Summary:
		//     The actual segments the stream was decomposed into.
		private Segment[] _segments;
		//
		// Summary:
		//     Disallow the default constructor as such object is illicit.
		private SegmentedStream() { }
		//
		// Summary:
		//     Creates a segmented stream from the provided source.
		// Parameters:
		//   source: The source stream to read file data from.
		//   compression: The selected compression type to use.
		private SegmentedStream(Stream source, Compression compression = Compression.GZCompression)
		{
			long originalSize = source.Length - source.Position;
			if (originalSize == 0)
			{
				throw new ArgumentException("Source stream is empty!");
			}

			// We want to avoid uneccessary allocations by allocating a large enough chunk of memory in time.
			// Unfortunately even though the size is known, in rare circumstances the compression may end up
			// increasing the original file size (e.g. when compressing a compressed stream).
			long conservativeSize = 2 * originalSize;
			using (MemoryStream compressedStream = new MemoryStream(new byte[conservativeSize]))
			{
				// Process compression, if any method is specified:
				Compress(source, compressedStream, compression);

				// This value is used often and rather verbose, used to improve readability.
				const int SegmentSize = Constants.Files.MaximumSegmentSizeInBytes;

				// Store the compressed length, the original buffer is conservative and will not represent the actual
				// length of the compressed stream!
				long compressedLength = compressedStream.Position;

				// Return to the beginning of the compressed stream and build individual segments:
				compressedStream.Seek(0, SeekOrigin.Begin);
				byte[] buffer = new byte[SegmentSize];

				// Determine the exact amount of segments required (based on number of full segments + remaining one).
				int segmentCount = (int)compressedLength / SegmentSize;
				if (compressedLength % SegmentSize > 0)
					++segmentCount;


				// Start copying the contents of the compressed stream into individual segments:
				Segment[] segments = new Segment[segmentCount];
				int index = 0;
				long remainingBytes = compressedLength;
				while (remainingBytes > 0)
				{
					int bytesRead = compressedStream.Read(buffer, 0, (int)Math.Min(remainingBytes, SegmentSize));

					segments[index] = new Segment(index, buffer, 0, bytesRead);

					++index;
					remainingBytes -= bytesRead;
				}

				// With all the segments created, finally compose the header and assign all data.
				_header = new Header(
					originalSize,
					segmentCount,
					compression
					);
				_segments = segments;
			}
		}
		//
		// Summary:
		//     Compresses the source stream into destination using the selected compression method.
		protected virtual void Compress(Stream source, Stream destination, Compression compression)
		{
			switch (compression)
			{
				case Compression.None:
					copyOnly();
					return;
				case Compression.GZCompression:
					compress();
					return;
				default:
					throw new NotImplementedException();
			}

			void copyOnly()
			{
				source.CopyTo(destination);
				destination.Flush();
			}

			void compress()
			{
				using (GZipStream compressionStream = new GZipStream(destination, CompressionMode.Compress, leaveOpen: true))
				{
					source.CopyTo(compressionStream);
					compressionStream.Flush();
				}
			}
		}
		//
		// Summary:
		//     Returns the size of the original stream that was segmented.
		public long Size
		{
			get
			{
				return _header.Size;
			}
		}
		//
		// Summary:
		//     Create segmented stream from the provided input stream.
		public static SegmentedStream CreateFrom(Stream source, Compression compression = Compression.GZCompression)
		{
			if (source == null || !source.CanRead)
				return null;

			return new SegmentedStream(source, compression);
		}
		//
		// Summary:
		//     Reconstruct the data from the existing segments and output it to the provided stream.
		public bool WriteTo(Stream destination)
		{
			if (destination == null || !destination.CanWrite)
				return false;

			// The destination must be large enough to fit the entire file uncompressed.
			long originalSize = Size;

			// Compose the entire compressed file from segments into the stream,
			// decompress it and then copy it to the destination:
			using (MemoryStream buffer = new MemoryStream(new byte[originalSize]))
			{
				// The stream was compressed and only then segmented, to the same in inverse:
				// Recreate the compressed stream from segments and then decompress:
				foreach (Segment segment in _segments)
					buffer.Write(segment.Data);

				// Flush any pending changes and return to the beginning of the compressed buffer.
				// Start the decompression, but leave the stream open.
				buffer.Seek(0, SeekOrigin.Begin);
				Decompress(buffer, destination, _header.Compression);
				destination.Flush();
				return true;
			}
		}
		//
		// Summary:
		//     Decompresses the source stream into destination using the selected compression method.
		protected virtual void Decompress(Stream source, Stream destination, Compression compression)
		{
			switch (compression)
			{
				case Compression.None:
					copyOnly();
					return;
				case Compression.GZCompression:
					decompress();
					return;

				default:
					throw new NotImplementedException();
			}

			void copyOnly()
			{
				source.CopyTo(destination);
				destination.Flush();
			}

			void decompress()
			{
				using (GZipStream decompressionStream = new GZipStream(source, CompressionMode.Decompress, true))
				{
					decompressionStream.CopyTo(destination);
					decompressionStream.Flush();
				}
			}
		}
	}
}
