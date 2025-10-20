using Vintagestory.API.Common;
using Instruments.Files;

namespace Instruments.Playback
{
	//
	// Summary:
	//     Base class for managing songs playback.
	public abstract class PlaybackManager
	{
		//
		// Summary:
		//     Creates new playback manager.
		public PlaybackManager(ICoreAPI api, FileManager fileManager) { }
		//
		// Summary:
		//     Updates the playback manager. This method should be called periodically, on each game tick.
		public virtual void Update(float deltaTime) { }
	}
}
