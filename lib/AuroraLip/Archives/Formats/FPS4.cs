using AuroraLip.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace AuroraLip.Archives.Formats
{
    // Tales of Graces archive format
    public class FPS4 : Archive, IMagicIdentify, IFileAccess
    {
        public struct ContentInfo
        {
            public ushort Value { get; private set; }
            public ContentInfo(ushort contentBitmask)
            {
                Value = contentBitmask;
            }

            // -- bitmask examples --
            // 0x000F -> loc, end, size, name
            // 0x0007 -> loc, end, size
            // 0x0047 -> loc, end, size, ptr to path? attributes? something like that
            public bool ContainsStartPointers { get { return (Value & 0x0001) == 0x0001; } }
            public bool ContainsSectorSizes { get { return (Value & 0x0002) == 0x0002; } }
            public bool ContainsFileSizes { get { return (Value & 0x0004) == 0x0004; } }
            public bool ContainsFilenames { get { return (Value & 0x0008) == 0x0008; } }
            public bool ContainsFiletypes { get { return (Value & 0x0020) == 0x0020; } }
            public bool ContainsFileMetadata { get { return (Value & 0x0040) == 0x0040; } }
            public bool Contains0x0080 { get { return (Value & 0x0080) == 0x0080; } }
            public bool Contains0x0100 { get { return (Value & 0x0100) == 0x0100; } }

            public bool HasUnknownDataTypes { get { return (Value & 0xFE10) != 0; } }
        }

        public class FileInfo
        {
            public uint FileIndex;
            public uint? Location = null;
            public uint? SectorSize = null;
            public uint? FileSize = null;
            public string FileName = null;
            public string FileType = null;
            public List<(string Key, string Value)> Metadata = null;
            public uint? Unknown0x0080 = null;
            public uint? Unknown0x0100 = null;

            public bool ShouldSkip => (Location != null && Location == 0xFFFFFFFF) || (Unknown0x0080 != null && Unknown0x0080 > 0);

            public FileInfo(Stream stream, uint fileIndex, ContentInfo bitmask, Endian endian)
            {
                FileIndex = fileIndex;
                if (bitmask.ContainsStartPointers)
                {
                    Location = stream.ReadUInt32(endian);
                }
                if (bitmask.ContainsSectorSizes)
                {
                    SectorSize = stream.ReadUInt32(endian);
                }
                if (bitmask.ContainsFileSizes)
                {
                    FileSize = stream.ReadUInt32(endian);
                }
                if (bitmask.ContainsFilenames)
                {
                    FileName = stream.ReadString(0x20);
                }
                if (bitmask.ContainsFiletypes)
                {
                    FileType = stream.ReadString(0x04);
                }
                if (bitmask.ContainsFileMetadata)
                {
                    uint pathLocation = stream.ReadUInt32(endian);
                    if (pathLocation != 0)
                    {
                        long previous_position = stream.Position;
                        stream.Seek(pathLocation, SeekOrigin.Begin);
                        string md = stream.ReadString();
                        stream.Seek(previous_position, SeekOrigin.Begin);
                        Metadata = new List<(string Key, string Value)>();
                        foreach (string m in md.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (m.Contains("="))
                            {
                                var s = m.Split(new char[] { '=' }, 2);
                                Metadata.Add((s[0], s[1]));
                            }
                            else
                            {
                                Metadata.Add((null, m));
                            }
                        }
                    }
                }
                if (bitmask.Contains0x0080)
                {
                    Unknown0x0080 = stream.ReadUInt32(Endian.Big);
                }
                if (bitmask.Contains0x0100)
                {
                    Unknown0x0100 = stream.ReadUInt32(Endian.Big);
                }
            }

            public uint? GuessFileSize(List<FileInfo> files)
            {
                uint? r = FileSize ?? SectorSize;
                if (r != null)
                {
                    return r;
                }

                if (Location != null && files != null)
                {
                    for (int i = (int)(FileIndex + 1); i < files.Count; ++i)
                    {
                        if (!files[i].ShouldSkip)
                        {
                            return files[i].Location - Location;
                        }
                    }
                }

                return null;
            }

            public override string ToString()
            {
                return FileName + " at 0x" + Location?.ToString("X8") + ", " + FileSize + " bytes";
            }
        }

        public bool CanRead => true;

        public bool CanWrite => false;

        public string Magic => magic;

        public static string magic = "FPS4";

        public FPS4() { }

        public FPS4(string filename) : base(filename) { }

        public FPS4(Stream stream, string filename = null) : base(stream, filename) { }

        public bool IsMatch(Stream stream, in string extension = "")
        {
            if (extension.ToLower().StartsWith(".tex"))
                return false;

            return stream.MatchString(FPS4.magic);
        }

        public static List<(long offset, uint size, string filename)> ProcessStream(Stream stream)
        {
            Endian endian = Endian.Big;
            uint file_count = stream.ReadUInt32(endian);
            uint header_size = stream.ReadUInt32(endian);

            // if header seems huge then we probably have assumed the wrong endianness
            if (header_size > 0xFFFF)
            {
                endian = Endian.Little;
                file_count = stream.ReadUInt32(endian);
                header_size = stream.ReadUInt32(endian);
            }

            uint first_file_start = stream.ReadUInt32(endian);
            ushort entry_size = stream.ReadUInt16(endian);
            ContentInfo content_bitmask = new ContentInfo(stream.ReadUInt16(endian));
            uint unknown2 = stream.ReadUInt32(endian);
            uint archive_name_location = stream.ReadUInt32(endian);
            stream.Seek(archive_name_location, SeekOrigin.Begin);
            string archive_name = stream.ReadString();

            List<FileInfo> files = new List<FileInfo>();
            for (uint i = 0; i < file_count; i++)
            {
                stream.Seek(header_size + (i * entry_size), SeekOrigin.Begin);
                files.Add(new FileInfo(stream, i, content_bitmask, endian));
            }

            bool should_guess_filesize_from_next_file = !content_bitmask.ContainsFileSizes && !content_bitmask.ContainsSectorSizes && CalculateIsLinear(content_bitmask, files);

            List<(long, uint, string)> data_offset_sizes = new List<(long, uint, string)>();
            for (uint i = 0; i < file_count; i++)
            {
                FileInfo file_info = files[(int)i];
                if (file_info.Location == null)
                {
                    throw new Exception("FPS4 extraction failure: Doesn't contain file start pointers!");
                }
                long file_offset = (long)(file_info.Location.Value);

                uint? maybeFilesize = file_info.GuessFileSize(should_guess_filesize_from_next_file ? files : null);
                if (maybeFilesize == null)
                {
                    throw new Exception("FPS4 extraction failure: Doesn't contain filesize information!");
                }
                if (maybeFilesize == 0)
                    continue;

                uint file_size = maybeFilesize.Value;

                data_offset_sizes.Add((file_offset, file_size, file_info.FileName));
            }

            return data_offset_sizes;
        }

        protected override void Read(Stream stream)
        {
            if (!stream.MatchString(magic))
                throw new InvalidIdentifierException(Magic);

            Root = new ArchiveDirectory() { OwnerArchive = this };
            int i = 0;
            foreach (var item in ProcessStream(stream))
            {
                stream.Seek(item.offset, SeekOrigin.Begin);

                //If Duplicate...
                string name = item.filename;
                if (name == null)
                    continue;
                if (Root.Items.ContainsKey(name)) name = i.ToString() + name;

                ArchiveFile Sub = new ArchiveFile() { Parent = Root, Name = name };
                Sub.FileData = new SubStream(stream, item.size);
                Root.Items.Add(Sub.Name, Sub);
                i++;
            }
        }

        private static bool CalculateIsLinear(ContentInfo content_bitmask, List<FileInfo> files)
        {
            if (content_bitmask.ContainsStartPointers)
            {
                uint lastFilePosition = files[0].Location.Value;
                for (int i = 1; i < files.Count; ++i)
                {
                    FileInfo file_info = files[i];
                    if (file_info.ShouldSkip)
                    {
                        continue;
                    }
                    if (file_info.Location.Value <= lastFilePosition)
                    {
                        return false;
                    }
                    lastFilePosition = file_info.Location.Value;
                }
                return true;
            }
            return false;
        }

        protected override void Write(Stream ArchiveFile)
        {
            throw new NotImplementedException();
        }
    }
}
