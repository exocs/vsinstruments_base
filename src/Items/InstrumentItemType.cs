using Instruments.Mapping;
using Midi;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Instruments.Items
{
	//
	// Summary:
	//     This object decouples and implements sharing of static data and logic of unique item types.
	//     This allows relevant data and logic to be accessed regardless of the actual item's lifetime.
	public class InstrumentItemType // TODO@exocs: Will need to move lower, for music block too?
	{
		//
		// Summary:
		//     Default shared item type, generally used if no other item type is provided.
		private static InstrumentItemType _defaultType = new InstrumentItemType("none", "holdbothhands");
		//
		// Summary:
		//     Default shared item type, generally used if no other item type is provided.
		private NoteMapping<string> _noteMap;
		//
		// Summary:
		//     Unique identifier of this item that can substitute its name.
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
		//     Tool modes shared across all instances of this instrument type.
		private SkillItem[] _toolModes;
		//
		// Summary:
		//     Link to the next registered item type or null if the last element.
		private InstrumentItemType _nextType;
		//
		// Summary:
		//     Whether this type has already been initialized or not.
		private bool _initialized;
		//
		// Summary:
		//     Returns the last created instrument type.
		private static InstrumentItemType LastType
		{
			get
			{
				InstrumentItemType type = InstrumentItemType._defaultType;
				while (type != null && type._nextType != null)
					type = type._nextType;
				return type;
			}
		}

		//
		// Summary:
		//     Iterates through and raises the callback for each registered instrument type.
		//
		// Parameters:
		//   callback: Callback raised for each type in the collection. Breaks when the callback returns false.
		private static void Foreach(System.Func<InstrumentItemType, bool> callback)
		{
			InstrumentItemType type = _defaultType;
			while (type != null)
			{
				if (!callback.Invoke(type))
					break;

				type = type._nextType;
			}
		}

		//
		// Summary:
		//     Initializes all registered types.
		public static void InitializeTypes(ICoreAPI api)
		{
			Foreach((InstrumentItemType itemType) =>
			{
				if (!itemType._initialized)
					itemType.Initialize(api);
				return true;
			});
		}

		//
		// Summary:
		//     De-initializes all registered types.
		public static void CleanupTypes()
		{
			Foreach((InstrumentItemType itemType) =>
			{
				if (itemType._initialized)
					itemType.Cleanup();
				return true;
			});
		}

		//
		// Summary:
		//     Create new shared instrument item type object.
		//
		// Parameters:
		//   instrumentName: Name of the instrument, as specified in the data, for example 'grandpiano'.
		//   animationName: Name of the animation used, as specified in the data, for example 'holdbothhands'.
		public InstrumentItemType(string instrumentName, string animationName)
		{
			_name = instrumentName;
			_animation = animationName;
			_initialized = false;
			_noteMap = new NoteMappingLegacy(string.Concat("sounds/", instrumentName));
			_id = instrumentName.GetHashCode();

			// Assign the linked list entry, so all instrument types can be iterated
			InstrumentItemType type = LastType;
			if (type != null)
				type._nextType = this;
		}

		//
		// Summary:
		//     Initializes this type.
		private void Initialize(ICoreAPI api)
		{
			_toolModes = ObjectCacheUtil.GetOrCreate(api, "instrumentToolModes", () =>
			{
				SkillItem[] modes = new SkillItem[4];
				modes[(int)PlayMode.abc] = new SkillItem() { Code = new AssetLocation(PlayMode.abc.ToString()), Name = Lang.Get("ABC Mode") };
				modes[(int)PlayMode.fluid] = new SkillItem() { Code = new AssetLocation(PlayMode.fluid.ToString()), Name = Lang.Get("Fluid Play") };
				modes[(int)PlayMode.lockedSemiTone] = new SkillItem() { Code = new AssetLocation(PlayMode.lockedSemiTone.ToString()), Name = Lang.Get("Locked Play: Semi Tone") };
				modes[(int)PlayMode.lockedTone] = new SkillItem() { Code = new AssetLocation(PlayMode.lockedTone.ToString()), Name = Lang.Get("Locked Play: Tone") };

				if (api != null && api is ICoreClientAPI capi)
				{
					modes[(int)PlayMode.abc].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/abc.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
					modes[(int)PlayMode.abc].TexturePremultipliedAlpha = false;
					modes[(int)PlayMode.fluid].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/3.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
					modes[(int)PlayMode.fluid].TexturePremultipliedAlpha = false;
					modes[(int)PlayMode.lockedSemiTone].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/2.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
					modes[(int)PlayMode.lockedSemiTone].TexturePremultipliedAlpha = false;
					modes[(int)PlayMode.lockedTone].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("instruments", "textures/icons/1.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
					modes[(int)PlayMode.lockedTone].TexturePremultipliedAlpha = false;
				}
				return modes;
			}
			);
			_initialized = true;
		}

		//
		// Summary:
		//     Releases any resources held by this type.
		private void Cleanup()
		{
			foreach (SkillItem toolMode in _toolModes)
				toolMode.Dispose();
			Array.Clear(_toolModes);

			_initialized = false;
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
		//     Returns whether this type is the default (dummy) type.
		internal bool IsDefault
		{
			get
			{
				return this == _defaultType;
			}
		}
		//
		// Summary:
		//     Returns the default instrument type object.
		internal static InstrumentItemType DefaultType
		{
			get
			{
				return _defaultType;
			}
		}

		//
		// Summary:
		//     Finds the instrument item type by its unique identifier.
		internal static InstrumentItemType Find(int id)
		{
			InstrumentItemType type = null;

			Foreach((InstrumentItemType predicate) =>
			{
				if (predicate._id == id)
				{
					type = predicate;
					return false;
				}
				return true;
			});

			return type;
		}

		//
		// Summary:
		//     Finds the instrument item type by its name.
		//     Only for compatibility purposes!
		[Obsolete("Prefer using InstrumentItemType.ID instead!")]
		internal static InstrumentItemType Find(string name)
		{
			int id = name.GetHashCode();
			return InstrumentItemType.Find(id);
		}
	}
}
