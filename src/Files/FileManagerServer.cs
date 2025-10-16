using Vintagestory.API.Server;

namespace Instruments.Files
{
	//
	// Summary:
	//     This class handles file transfers on the server side.
	public class FileManagerServer : FileManager
	{
		//	 
		// Summary:	 
		//     Returns the interface to the game.
		protected new ICoreServerAPI Api { get; }
		//
		// Summary:
		//     Creates new file manager.
		// Parameters:
		//   api: The game interface.
		//   root: Root directory this manager will operate in.
		public FileManagerServer(ICoreServerAPI api, string root) :
			base(api, root)
		{
			Api = api;
		}
	}
}
