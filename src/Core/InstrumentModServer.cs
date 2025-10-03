using System.Collections.Generic; // List
using System.IO; // Open files
using Vintagestory.API.Common;
using Vintagestory.API.Server;

using Instruments.Network.Packets;
using Instruments.Blocks;
using Instruments.Items;

namespace Instruments.Core
{
    public class InstrumentModServer : InstrumentModCommon
    {
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }
        #region SERVER
        private ICoreServerAPI serverAPI;
        IServerNetworkChannel serverChannelNote;
        IServerNetworkChannel serverChannelABC;

        long listenerID = -1;
        string abcBaseDir;

        private struct PlaybackData
        {
            public int ClientID;
            public string abcData;
            public ABCParser parser;
            public int index;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            serverAPI = api;
            base.StartServerSide(api);
            serverChannelNote =
                api.Network.RegisterChannel("noteTest")
                .RegisterMessageType(typeof(NoteStart))
                .RegisterMessageType(typeof(NoteUpdate))
                .RegisterMessageType(typeof(NoteStop))
                .SetMessageHandler<NoteStart>(RelayMakeNote)
                .SetMessageHandler<NoteUpdate>(RelayUpdateNote)
                .SetMessageHandler<NoteStop>(RelayStopNote)
                ;
            serverChannelABC =
                api.Network.RegisterChannel("abc")
                .RegisterMessageType(typeof(ABCStartFromClient))
                .RegisterMessageType(typeof(ABCStopFromClient))
                .RegisterMessageType(typeof(ABCUpdateFromServer))
                .RegisterMessageType(typeof(ABCStopFromServer))
                .RegisterMessageType(typeof(ABCSendSongFromServer))
                .SetMessageHandler<ABCStartFromClient>(StartABC)
                .SetMessageHandler<ABCStopFromClient>(StopABC)
                .SetMessageHandler<ABCStopFromServer>(null)
                .SetMessageHandler<ABCUpdateFromServer>(null)
                ;

            serverAPI.Event.RegisterGameTickListener(OnServerGameTick, 1); // arg1 is millisecond Interval
            MusicBlockManager.GetInstance().Reset();
            ABCParsers.GetInstance().SetAPI(serverAPI);

            
            serverAPI.Event.PlayerJoin += SendSongs;
        }
        public override void Dispose()
        {
            // We MIGHT need this when resetting worlds without restarting the game
            base.Dispose();
            if (listenerID != -1)
            {
                serverAPI.Event.UnregisterGameTickListener(listenerID);
                listenerID = 0;
            }
            ABCParsers.GetInstance().Reset();
        }
        public void SendSongs(IServerPlayer byPlayer)
        {
            string serverDir = config.abcServerLocation;
            if (!RecursiveFileProcessor.DirectoryExists(serverDir))
                return; // Server has no abcs, do nothing

            List<string> abcFiles = new List<string>();
            RecursiveFileProcessor.ProcessDirectory(serverDir, serverDir + Path.DirectorySeparatorChar, ref abcFiles);
            if (abcFiles.Count == 0)
            {
                return; // No files in the folder
            }
            foreach (string song in abcFiles)
            {
                ABCSendSongFromServer packet = new ABCSendSongFromServer();
                packet.abcFilename = song;
                serverChannelABC.SendPacket(packet, byPlayer);
            }
        }
        private void RelayMakeNote(IPlayer fromPlayer, NoteStart note)
        {
            // Send A packet to all clients (or clients within the area?) to start a note
            note.ID = fromPlayer.ClientId;
            serverChannelNote.BroadcastPacket(note);
        }
        private void RelayUpdateNote(IPlayer fromPlayer, NoteUpdate note)
        {
            // Send A packet to all clients (or clients within the area?) to start a note
            note.ID = fromPlayer.ClientId;
            serverChannelNote.BroadcastPacket(note);
        }
        private void RelayStopNote(IPlayer fromPlayer, NoteStop note)
        {
            // Send A packet to all clients (or clients within the area?) to start a note
            note.ID = fromPlayer.ClientId;
            serverChannelNote.BroadcastPacket(note);
        }
        private void StartABC(IPlayer fromPlayer, ABCStartFromClient abcData)
        {
            ABCParser abcp = ABCParsers.GetInstance().FindByID(fromPlayer.ClientId);
            if (abcp == null)
            {
                string abcSong = "";
                if (abcData.isServerFile)
                {
                    // The contained string is NOT a full song, but a link to it on the server.
                    // Find this file, load it, and make the abcParser in the same way
                    string fileLocation = config.abcServerLocation;
                    RecursiveFileProcessor.ReadFile(fileLocation + Path.DirectorySeparatorChar + abcData.abcData, ref abcSong);
                }
                else
                {
                    abcSong = abcData.abcData;
                }

                ABCParsers.GetInstance().MakeNewParser(serverAPI, fromPlayer, abcSong, abcData.bandName, abcData.instrument);
                if (serversideAnimSync)
                    fromPlayer?.Entity?.StartAnimation(Definitions.GetInstance().GetAnimation(abcData.instrument));
            }
            else
            {
                ABCParsers.GetInstance().Remove(serverAPI, fromPlayer, abcp);
            }
            /*
            if (listenerID == -1)
            {
                listenerID = serverAPI.Event.RegisterGameTickListener(OnServerGameTick, 1); // arg1 is millisecond Interval
            }
            */
        }
        private void StopABC(IPlayer fromPlayer, ABCStopFromClient abcData)
        {
            int clientID = fromPlayer.ClientId;
            ABCParser abcp = ABCParsers.GetInstance().FindByID(clientID);
            if (abcp != null)
            {
                ABCParsers.GetInstance().Remove(serverAPI, fromPlayer, abcp);
                ABCStopFromServer packet = new ABCStopFromServer();
                packet.fromClientID = clientID;
                IServerNetworkChannel ch = serverAPI.Network.GetChannel("abc");
                ch.BroadcastPacket(packet);

                if(serversideAnimSync)
                    fromPlayer?.Entity?.StopAnimation(Definitions.GetInstance().GetAnimation(abcp.instrument));
            }

            return;
        }
        private void OnServerGameTick(float dt)
        {
            ABCParsers.GetInstance().Update(serverAPI, dt);
        }
        #endregion
    }
}