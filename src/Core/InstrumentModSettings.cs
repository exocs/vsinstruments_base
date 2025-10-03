﻿using System.IO;

namespace Instruments.Core
{
    public class InstrumentModSettings
    {
        public bool enabled { get; set; } = true;
        public float playerVolume { get; set; } = 0.7f;
        public float blockVolume { get; set; } = 1.0f;
        public int abcBufferSize { get; set; } = 32;
        public string abcLocalLocation { get; set; } = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "abc";
        public string abcServerLocation { get; set; } = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "abc_server";
    }
}