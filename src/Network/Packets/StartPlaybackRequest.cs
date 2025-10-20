using ProtoBuf;

namespace Instruments.Network.Packets
{
	//
	// Summary:
	//     Packet sent to the server from clients to request playback.
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class StartPlaybackRequest
	{
		//
		// Summary:
		//     Relative path to the file to be played.
		public string File;
		//
		// Summary:
		//     The channel index to start playing.
		public int Channel;
		//
		// Summary:
		//     The unique identifier of instrument type used.
		public int Instrument;
	}
}