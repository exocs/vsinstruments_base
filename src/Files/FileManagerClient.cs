using Vintagestory.API.Client;

namespace Instruments.Files
{
	//
	// Summary:
	//     This class handles file transfers on the client side.
	public class FileManagerClient : FileManager
	{
		//	 
		// Summary:	 
		//     Returns the interface to the game.
		protected new ICoreClientAPI Api { get; }
		//
		// Summary:
		//     Creates new file manager.
		// Parameters:
		//   api: The game interface.
		//   root: Root directory this manager will operate in.
		public FileManagerClient(ICoreClientAPI api, string root) :
			base(api, root)
		{
			Api = api;
		}
	}
}
