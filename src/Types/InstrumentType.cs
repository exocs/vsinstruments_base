using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Midi;
using Instruments.Items;
using Instruments.Mapping;

namespace Instruments.Types
{
	//
	// Summary:
	//     This object decouples and implements sharing of static data and logic of unique item types.
	//     This allows relevant data and logic to be accessed regardless of the actual item's lifetime.
	//     See InstrumentTypeExtensions for convenience API extensions.
	public abstract class InstrumentType
	{
		//
		// Summary:
		//     Structure containing data for initializationg of an InstrumentType.
		public struct InitArgs
		{
			public string Name;
			public string Animation;
			// User provided argument (optional)
			public object Extra;

			public InitArgs(string name, string animation, object extra = null)
			{
				Name = name;
				Animation = animation;
				Extra = extra;
			}
		}
		//
		// Summary:
		//     Unique identifier of this instrument type.
		private int _id;
		// Summary:
		//     Name of the instrument as specified in the data, for example 'grandpiano'.
		private string _name;
		//
		// Summary:
		//     Name of the animation as specified in the data, for example 'holdbothhandslarge'.
		private string _animation;
		//
		// Summary:
		//     Default shared item type, generally used if no other item type is provided.
		private NoteMapping<string> _noteMap;
		//
		// Summary:
		//     Tool modes shared across all instances of this instrument type.
		private SkillItem[] _toolModes;
		//
		// Summary:
		//     Map of all instrument types by their unique identifier.
		private static Dictionary<int, InstrumentType> _instrumentTypes;
		//
		// Summary:
		//     Intializes static type properties.
		static InstrumentType()
		{
			_instrumentTypes = new Dictionary<int, InstrumentType>();
		}
		//
		// Summary:
		//     Registers and associates the provided class type with given instance type.
		internal static void RegisterType(ICoreAPI api, Type instanceType, InstrumentType classType, InitArgs initArgs)
		{
			int id = ComputeID(instanceType);
			if (_instrumentTypes.TryAdd(id, classType))
			{
				classType._id = id;
				classType._name = initArgs.Name;
				classType._animation = initArgs.Animation;
				classType.Initialize(api, initArgs);
			}
		}
		//
		// Summary:
		//     Initializes this type.
		protected virtual void Initialize(ICoreAPI api, InitArgs initArgs)
		{
			_toolModes = new SkillItem[4];
			_toolModes[(int)PlayMode.abc] = new SkillItem() { Code = new AssetLocation(PlayMode.abc.ToString()), Name = Lang.Get("ABC Mode") };
			_toolModes[(int)PlayMode.fluid] = new SkillItem() { Code = new AssetLocation(PlayMode.fluid.ToString()), Name = Lang.Get("Fluid Play") };
			_toolModes[(int)PlayMode.lockedSemiTone] = new SkillItem() { Code = new AssetLocation(PlayMode.lockedSemiTone.ToString()), Name = Lang.Get("Locked Play: Semi Tone") };
			_toolModes[(int)PlayMode.lockedTone] = new SkillItem() { Code = new AssetLocation(PlayMode.lockedTone.ToString()), Name = Lang.Get("Locked Play: Tone") };

			if (api is ICoreClientAPI capi)
			{
				_toolModes[(int)PlayMode.abc].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/abc.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
				_toolModes[(int)PlayMode.abc].TexturePremultipliedAlpha = false;
				_toolModes[(int)PlayMode.fluid].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/3.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
				_toolModes[(int)PlayMode.fluid].TexturePremultipliedAlpha = false;
				_toolModes[(int)PlayMode.lockedSemiTone].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/2.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
				_toolModes[(int)PlayMode.lockedSemiTone].TexturePremultipliedAlpha = false;
				_toolModes[(int)PlayMode.lockedTone].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/1.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
				_toolModes[(int)PlayMode.lockedTone].TexturePremultipliedAlpha = false;
			}

			_noteMap = new NoteMappingLegacy(string.Concat("sounds/", Name));
		}
		//
		// Summary:
		//     Unregister this type.
		internal static void UnregisterType(Type instanceType)
		{
			int typeID = ComputeID(instanceType);
			if (_instrumentTypes.Remove(typeID, out InstrumentType type))
			{
				type.Cleanup();
			}
		}
		//
		// Summary:
		//     Releases any resources held by this type.
		protected virtual void Cleanup()
		{
			foreach (SkillItem toolMode in _toolModes)
				toolMode.Dispose();
			Array.Clear(_toolModes);
			_toolModes = null;
		}
		//
		// Summary:
		//     Returns the unique identifier of this instrument that can be used as a substitute for its name.
		public int ID
		{
			get
			{
				return _id;
			}
		}

		//
		// Summary:
		//     Returns the name of this instrument, as defined in data.
		public string Name
		{
			get
			{
				return _name;
			}
		}
		//
		// Summary:
		//     Returns the name of the animation used by this instrument, as defined in data.
		public string Animation
		{
			get
			{
				return _animation;
			}
		}
		//
		// Summary:
		//     Returns the note mapping for this instrument type.
		public NoteMapping<string> NoteMap
		{
			get
			{
				return _noteMap;
			}
		}
		//
		// Summary:
		//     Returns the tool modes for this instrument type.
		public SkillItem[] ToolModes
		{
			get
			{
				return _toolModes;
			}
		}
		//
		// Summary:
		//     Returns sound data of this instrument for the provided pitch.
		//
		// Parameters:
		//   pitch: Input pitch the sound should represent.
		//   assetPath: Outputs the path to the desired sound sample.
		//   modPitch: Outputs the pitch the sound sample should play at.
		public virtual bool GetPitchSound(Pitch pitch, out string assetPath, out float modPitch)
		{
			assetPath = NoteMap.GetValue(pitch);
			if (string.IsNullOrEmpty(assetPath))
			{
				modPitch = 1;
				return false;
			}

			modPitch = NoteMap.GetRelativePitch(pitch);
			return true;
		}
		//
		// Summary:
		//     Finds the instrument item type by its unique identifier.
		internal static InstrumentType Find(int id)
		{
			if (_instrumentTypes.TryGetValue(id, out InstrumentType type))
				return type;

			return null;
		}
		//
		// Summary:
		//     Finds the instrument item type by its instance type.
		internal static InstrumentType Find(Type type)
		{
			int typeID = ComputeID(type);
			return Find(typeID);
		}
		//
		// Summary:
		//     Iterates through all existing instrument types, raising the callback for each entry.
		//     Stops the iteration once the callback function returns true.
		//     This method is considered very inefficient and should be avoided whenever possible.
		internal static InstrumentType Foreach(System.Func<InstrumentType, bool> callback)
		{
			foreach (var keyValuePair in _instrumentTypes)
			{
				if (callback(keyValuePair.Value))
					return keyValuePair.Value;
			}
			return null;
		}
		//
		// Summary:
		//     Returns unique identifier for provided type.
		private static int ComputeID(Type type)
		{
			return type.FullName.GetHashCode();
		}
	}

	//
	// Summary:
	//     This class provides convenience and utility extensions for instrument types.
	public static class InstrumentTypeExtensions
	{
		//
		// Summary:
		//     Register instrument along with its associated instrument type class.
		//
		// Parameters:
		//   api: Game API
		//   name: Default instrument name
		//   animation: Default animation used by the instrument
		//   extra: User provided params
		public static void RegisterInstrumentItem<Instrument, InstrumentClass>(this ICoreAPI api, string name, string animation, object extra = null)
			where Instrument : InstrumentItem
			where InstrumentClass : InstrumentType, new()
		{
			Type itemType = typeof(Instrument);
			api.RegisterItemClass(name, itemType);
			InstrumentClass instrumentType = new InstrumentClass();
			InstrumentType.InitArgs initArgs = new InstrumentType.InitArgs(name, animation, extra);
			InstrumentType.RegisterType(api, itemType, instrumentType, initArgs);
		}
	}
}
