namespace Instruments
{
    public enum PlayMode
    {
        lockedTone = 0, // Player y angle floored to nearest tone
        lockedSemiTone, // Player y angle floored to nearest semi-tone
        fluid,      // Player y angle directly correlates to pitch
        abc         // Playing an abc file
    }

    public struct NoteFrequency
    {
        public string ID;
        public float pitch;
        public NoteFrequency(string id, float p)
        {
            this.ID = id;
            this.pitch = p;
        }
    }
}