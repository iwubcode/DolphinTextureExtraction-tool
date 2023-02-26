using AuroraLip.Archives.Formats;
using AuroraLip.Common;
using HyoutaTools.Tales.Vesperia.Texture;
using HyoutaTools.Textures.ColorFetchingIterators;
using HyoutaTools.Textures.PixelOrderIterators;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace AuroraLip.Texture.Formats
{
    public class TEX_TOG : JUTTexture, IFileAccess
    {
        public bool CanRead => true;

        public bool CanWrite => false;

        public string Extension => ".tex";

        public TEX_TOG() { }

        public TEX_TOG(Stream stream) : base(stream) { }

        public TEX_TOG(string filepath) : base(filepath) { }

        public static bool Matcher(Stream stream, in string extension = "")
        {
            if (!extension.ToLower().StartsWith(".tex"))
                return false;

            if (!stream.MatchString(FPS4.magic))
                return false;

            try
            {
                var container_data = FPS4.ProcessStream(stream);
                return container_data.Count == 2;
            }
            catch
            {
            }
            return false;
        }

        public bool IsMatch(Stream stream, in string extension = "")
            => Matcher(stream, in extension);

        protected override void Read(Stream stream)
        {
            if (!stream.MatchString(FPS4.magic))
                throw new InvalidIdentifierException(FPS4.magic);

            var container_data = FPS4.ProcessStream(stream);
            stream.Seek(container_data[0].offset, SeekOrigin.Begin);
            HyoutaTools.Tales.Vesperia.Texture.TXM txm = new HyoutaTools.Tales.Vesperia.Texture.TXM(stream);

            stream.Seek(container_data[1].offset, SeekOrigin.Begin);
            HyoutaTools.Tales.Vesperia.Texture.TXV txv = new HyoutaTools.Tales.Vesperia.Texture.TXV(txm, stream, null);

            foreach (var texture in txv.textures)
            {
                for (uint depth = 0; depth < texture.TXM.Depth; ++depth)
                {
                    Stream plane = texture.TXM.GetSinglePlane(stream, depth);
                    switch (texture.TXM.Format)
                    {
                        case TextureFormat.DXT1a:
                        case TextureFormat.DXT1b:
                        case TextureFormat.DXT5a:
                        case TextureFormat.DXT5b:
                            {
                                // TODO, convert to stream that is handled by the base tools?
                                continue;
                            }
                            break;
                    };
                    var dims = texture.TXM.GetDimensions(0);
                    if (!TEX_ImageFormat.ContainsKey(texture.TXM.Format))
                        continue;
                    TexEntry current = new TexEntry(plane, null, TEX_ImageFormat[texture.TXM.Format], GXPaletteFormat.IA8, 0, (int)dims.width, (int)dims.height, (int)texture.TXM.Mipmaps)
                    {
                        LODBias = 0,
                        MagnificationFilter = GXFilterMode.Nearest,
                        MinificationFilter = GXFilterMode.Nearest,
                        WrapS = GXWrapMode.CLAMP,
                        WrapT = GXWrapMode.CLAMP,
                        EnableEdgeLOD = false,
                        MinLOD = 0,
                        MaxLOD = 0
                    };
                    Add(current);
                }
            }
        }

        static Dictionary<HyoutaTools.Tales.Vesperia.Texture.TextureFormat, GXImageFormat> TEX_ImageFormat = new Dictionary<HyoutaTools.Tales.Vesperia.Texture.TextureFormat, GXImageFormat>
        {
            { HyoutaTools.Tales.Vesperia.Texture.TextureFormat.RGB565, GXImageFormat.RGB565 },
            { HyoutaTools.Tales.Vesperia.Texture.TextureFormat.GamecubeCMP, GXImageFormat.CMPR },
            { HyoutaTools.Tales.Vesperia.Texture.TextureFormat.GamecubeCMP2, GXImageFormat.CMPR },
            { HyoutaTools.Tales.Vesperia.Texture.TextureFormat.GamecubeCMP4, GXImageFormat.CMPR },
            { HyoutaTools.Tales.Vesperia.Texture.TextureFormat.GamecubeCMPA, GXImageFormat.CMPR },
            { HyoutaTools.Tales.Vesperia.Texture.TextureFormat.GamecubeCMPC, GXImageFormat.CMPR }
        };

        protected override void Write(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
