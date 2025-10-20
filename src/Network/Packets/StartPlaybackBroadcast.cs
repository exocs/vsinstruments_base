using ProtoBuf;

namespace Instruments.Network.Packets
{
	//
	// Summary:
	//     Packet broadcast to clients from the server to start a playback.
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class StartPlaybackBroadcast
	{
		//
		// Summary:
		//     Index of the player that started the playback.
		public int ClientId;
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