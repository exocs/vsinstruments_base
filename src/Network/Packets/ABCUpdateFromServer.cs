using ProtoBuf;
using Vintagestory.API.MathTools;

namespace Instruments.Network.Packets
{
	[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
	public class ABCUpdateFromServer
	{
		public Vec3d positon;
		public Chord newChord;
		public int fromClientID;
		public string instrument;
	}
}