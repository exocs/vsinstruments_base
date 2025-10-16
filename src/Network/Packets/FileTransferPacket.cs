using System.IO;
using System;
using ProtoBuf;
using Instruments.Files;

namespace Instruments.Network.Packets
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class FileTransferPacket
	{
		//
		// Summary:
		//     The relative file name. For security reasons, never use this field directly and make sure
		//     that the provided name is always sanitized first!
		public string Name;
		//
		// Summary:
		//     Original file size prior to any compression.
		public long Size;
		//
		// Summary:
		//     The compression method used.
		public CompressionMethod Compression;
		//
		// Summary:
		//     The actual file payload.
		public byte[] Data;
	}
}