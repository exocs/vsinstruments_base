using Instruments.Files;
using ProtoBuf;

namespace Instruments.Network.Packets
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class GetFileResponse
	{
		//
		// Summary:
		//     The unique identifier of this request.
		public int RequestID;
		//
		// Summary:
		//     Uncompressed (original) size.
		public int Size;
		//
		// Summary:
		//     Used compression size.
		public CompressionMethod Compression;
		//
		// Summary:
		//     Actual file data.
		public byte[] Data;
	}
}