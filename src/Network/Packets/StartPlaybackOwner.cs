using ProtoBuf;

namespace Instruments.Network.Packets
{
	//
	// Summary:
	//     Packet broadcast to the instigator of the playback.
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class StartPlaybackOwner
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