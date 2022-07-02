﻿using System;

namespace AuroraLip.Compression.Formats
{
    /// <summary>
    /// LZSS Lempel–Ziv–Storer–Szymanski algorithm, a derivative of LZ77.
    /// </summary>
    public class LZSS : ICompression
    {
        public bool CanCompress { get; } = false;

        public bool CanDecompress { get; } = false;

        public byte[] Compress(in byte[] Data)
        {
            throw new NotImplementedException();
        }

        public byte[] Decompress(in byte[] Data)
        {
            throw new NotImplementedException();
        }

        public bool IsMatch(in byte[] Data)
        {
            return Data.Length > 8 && Data[0] == 16;
        }
    }
}
