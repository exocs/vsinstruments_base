using System;
using Vintagestory.API.Common;

using Instruments.Blocks;

namespace Instruments.Core
{
    public class InstrumentModCommon : ModSystem
    {
        protected bool otherPlayerSync = true;
        protected bool serversideAnimSync = false;
        public static InstrumentSettings config;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockClass("musicblock", typeof(MusicBlock));
            api.RegisterBlockEntityClass("musicblockentity", typeof(BEMusicBlock));

            // Load settings file
            try
            {
                config = api.LoadModConfig<InstrumentSettings>("instruments.json");
                if (config == null)
                {
                    config = new InstrumentSettings();
                    api.StoreModConfig(config, "instruments.json");
                }
            }
            catch (Exception)
            {
                api.Logger.Error("Could not load instruments config, using default values...");
                config = new InstrumentSettings();
            }
        }
    }
}