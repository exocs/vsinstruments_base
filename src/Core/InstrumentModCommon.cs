using System;
using Vintagestory.API.Common;

using Instruments.Blocks;
using Instruments.Items;

namespace Instruments.Core
{
    public class InstrumentModCommon : ModSystem
    {
        protected bool otherPlayerSync = true;
        protected bool serversideAnimSync = false;
        public static InstrumentModSettings config;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockClass("musicblock", typeof(MusicBlock));
            api.RegisterBlockEntityClass("musicblockentity", typeof(BEMusicBlock));

            // Load settings file
            try
            {
                config = api.LoadModConfig<InstrumentModSettings>("instruments.json");
                if (config == null)
                {
                    config = new InstrumentModSettings();
                    api.StoreModConfig(config, "instruments.json");
                }
            }
            catch (Exception)
            {
                api.Logger.Error("Could not load instruments config, using default values...");
                config = new InstrumentModSettings();
            }
        }

		public override void AssetsLoaded(ICoreAPI api)
		{
			// Initialize all instrument types, these serve as a shared common
			// storage for all properties of a single instrument kind.
			InstrumentItemType.InitializeTypes(api);
		}

		public override void Dispose()
		{
			InstrumentItemType.CleanupTypes();
			base.Dispose();
		}
	}
}