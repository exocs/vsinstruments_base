using System;
using System.IO;
using Vintagestory.API.Common;

namespace Instruments.Core
{
	public class InstrumentModSettings
	{
		//
		// Summary:
		//     The settings instance. Only exists after loaded!
		private static InstrumentModSettings _instance;


		public bool enabled { get; set; } = true;
		public float playerVolume { get; set; } = 0.7f;
		public float blockVolume { get; set; } = 1.0f;
		public int abcBufferSize { get; set; } = 32;

		[Obsolete("Abc is no longer supported!")]
		public string abcLocalLocation { get; set; } = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "abc";

		[Obsolete("Abc is no longer supported!")]
		public string abcServerLocation { get; set; } = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "abc_server";


		//
		// Summary:
		//     Local fully qualified path to the directory in which midi files are stored.
		//     By default this points to the midi folder in the game directory.
		//     TODO@exocs: Implement per server directory for the client.
		public string ClientMidiDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "midi");

		//
		// Summary:
		//     Local fully qualified path to the directory in which server midi files are stored.
		//     By default this points to the midi folder in the game directory.
		//     TODO@exocs: Implement properly.
		public string ServerMidiDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "midi");

		//
		// Summary:
		//     Loads the mod configuration, or creates default configuration if no mod settings are present.
		public static void Load(ICoreAPI api)
		{
			// Load settings file
			try
			{
				InstrumentModSettings instance = api.LoadModConfig<InstrumentModSettings>("instruments.json");
				if (instance == null)
				{
					instance = new InstrumentModSettings();
					api.StoreModConfig(instance, "instruments.json");
				}

				_instance = instance;
			}
			catch (Exception)
			{
				api.Logger.Error("Could not load instruments config, using default values...");
				_instance = new InstrumentModSettings();
			}
		}

		//
		// Summary:
		//     Returns the loaded instrument mod settings instance.
		public static InstrumentModSettings Instance
		{
			get
			{
				if (_instance == null)
					throw new Exception("Mod settings instance must be loaded before it may be used!");

				return _instance;
			}
		}
	}
}