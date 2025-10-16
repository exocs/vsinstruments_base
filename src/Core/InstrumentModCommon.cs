using System;
using Vintagestory.API.Common;

using Instruments.Blocks;
using Instruments.Types;
using Instruments.Files;

namespace Instruments.Core
{
    public abstract class InstrumentModCommon : ModSystem
    {
        protected bool otherPlayerSync = true;
        protected bool serversideAnimSync = false;

        public abstract FileManager FileManager { get; }

		public override void Start(ICoreAPI api)
        {
            base.Start(api);

            InstrumentModSettings.Load(api);

            // TODO@exocs: Add InstrumentType support
            api.RegisterBlockClass("musicblock", typeof(MusicBlock));
            api.RegisterBlockEntityClass("musicblockentity", typeof(BEMusicBlock));
        }

		public override void AssetsLoaded(ICoreAPI api)
		{
			base.AssetsLoaded(api);
			InstrumentType.InitializeTypes();
		}
	}
}