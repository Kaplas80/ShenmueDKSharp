﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShenmueDKSharp.Files.Images
{
    public class BMP
    {
        public readonly static List<string> Extensions = new List<string>()
        {
            "BMP"
        };

        public readonly static List<byte[]> Identifiers = new List<byte[]>()
        {
            new byte[2] { 0x42, 0x4D }, //BM
        };
    }
}
