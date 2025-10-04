using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Instruments.Core;
using Midi;
using Note = Midi.Note;

namespace Instruments
{
	public class Definitions
	{
		private string bandName = "";
		private PlayMode mode = PlayMode.abc;
		private static Definitions _instance;
		//private Dictionary<int, NoteFrequency> noteMap = new Dictionary<int, NoteFrequency>();
		private Dictionary<string, string> animMap = new Dictionary<string, string>();
		private List<string> abcFiles = new List<string>();
		private List<string> serverAbcFiles = new List<string>();
		private bool messageDone = false;
		private bool abcPlaying = false;

		private Dictionary<string, string> instrumentTypes = new Dictionary<string, string>();

		private Definitions()
		{
			// Populate the dict
			/*int i = 0;
			noteMap.Add(i++, new NoteFrequency("a3", 0.5000f));
			noteMap.Add(i++, new NoteFrequency("a^3", 0.5295f));
			noteMap.Add(i++, new NoteFrequency("b3", 0.5614f));
			noteMap.Add(i++, new NoteFrequency("c3", 0.5945f));
			noteMap.Add(i++, new NoteFrequency("c^3", 0.6300f));
			noteMap.Add(i++, new NoteFrequency("d3", 0.6672f));
			noteMap.Add(i++, new NoteFrequency("d^3", 0.7073f));
			noteMap.Add(i++, new NoteFrequency("e3", 0.7491f));
			noteMap.Add(i++, new NoteFrequency("f3", 0.7936f));
			noteMap.Add(i++, new NoteFrequency("f^3", 0.8409f));
			noteMap.Add(i++, new NoteFrequency("g3", 0.8909f));
			noteMap.Add(i++, new NoteFrequency("g^3", 0.9441f));
			noteMap.Add(i++, new NoteFrequency("a4", 1.0000f));
			noteMap.Add(i++, new NoteFrequency("a^4", 1.0595f));
			noteMap.Add(i++, new NoteFrequency("b4", 1.1223f));
			noteMap.Add(i++, new NoteFrequency("c3", 1.1891f));
			noteMap.Add(i++, new NoteFrequency("c^4", 1.2600f));
			noteMap.Add(i++, new NoteFrequency("d4", 1.335f));
			noteMap.Add(i++, new NoteFrequency("d^4", 1.4141f));
			noteMap.Add(i++, new NoteFrequency("e4", 1.4964f));
			noteMap.Add(i++, new NoteFrequency("f4", 1.5873f));
			noteMap.Add(i++, new NoteFrequency("f^4", 1.6818f));
			noteMap.Add(i++, new NoteFrequency("g4", 1.7818f));
			noteMap.Add(i++, new NoteFrequency("g^4", 1.8877f));
			noteMap.Add(i++, new NoteFrequency("a5", 2.0000f));*/

			instrumentTypes.Add("none", "none");  // Dummy value 
		}

		[Obsolete("Use Instance instead!")]
		public static Definitions GetInstance()
		{
			if (_instance != null)
				return _instance;
			return _instance = new Definitions();
		}

		public static Definitions Instance
		{
			get
			{
				return _instance != null ? _instance : _instance = new Definitions();
			}
		}

		public void SetBandName(string bn)
		{
			bandName = bn;
		}
		public string GetBandName()
		{
			return bandName;
		}
		public void SetPlayMode(PlayMode newMode)
		{
			mode = newMode;
		}
		public PlayMode GetPlayMode()
		{
			return mode;
		}
		[Obsolete("Use Pitch and its extension API instead!", false)]
		public NoteFrequency GetFrequency(int index)
		{
			Pitch pitch = Pitch.A3 + index;
			Midi.Note note = pitch.NotePreferringSharps();
			return new NoteFrequency(note.ToString(), pitch.Frequency() / Pitch.A3.Frequency());
		}
		public List<string> GetSongList()
		{
			return abcFiles;
		}
		public string GetAnimation(string type)
		{
			return instrumentTypes[type];
		}
		public void AddInstrumentType(string type, string anim)
		{
			if (!instrumentTypes.ContainsKey(type))
				instrumentTypes.Add(type, anim);
		}
		public Dictionary<string, string> GetInstrumentTypes()
		{
			return instrumentTypes;
		}
		public bool UpdateSongList(ICoreClientAPI capi)
		{
			abcFiles.Clear();
			// First, check the client's dir exists
			string localDir = InstrumentModCommon.config.abcLocalLocation;
			if (RecursiveFileProcessor.DirectoryExists(localDir))
			{
				// It exists! Now find the files in it
				RecursiveFileProcessor.ProcessDirectory(localDir, localDir + Path.DirectorySeparatorChar, ref abcFiles);
			}
			else
			{
				if (!messageDone)
				{
					// Client ABC folder not found, log a message to tell the player where it should be. But still search the server folder
					capi.ShowChatMessage("ABC warning: Could not find folder at \"" + localDir + "\". Displaying server files instead.");
					messageDone = true;
				}
			}
			foreach (string song in serverAbcFiles)
				abcFiles.Add(song);

			if (abcFiles.Count == 0)
			{
				capi.ShowChatMessage("ABC error: No abc files found!");
				return false;
			}
			else
			{
				return true;
			}
		}
		public void AddToServerSongList(string songFileName)
		{
			serverAbcFiles.Add(songFileName);
		}
		public string ABCBasePath()
		{
			return InstrumentModCommon.config.abcLocalLocation;
		}
		public void SetIsPlaying(bool toggle)
		{
			abcPlaying = toggle;
		}
		public bool IsPlaying() { return abcPlaying; }
		public void Reset()
		{
			abcFiles.Clear();
			serverAbcFiles.Clear();
		}
	}
}
