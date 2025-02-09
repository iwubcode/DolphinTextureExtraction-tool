﻿using AuroraLib.Common;

namespace AuroraLib.Texture
{
    /*
    * base on https://github.com/SuperHackio/Hack.io
    */

    /// <summary>
    /// The base class of all textures.
    /// </summary>
    public abstract partial class JUTTexture : List<JUTTexture.TexEntry>, IDisposable, IName
    {
        /// <summary>
        /// The full path of this file.
        /// </summary>
        public string Name { get; set; } = null;

        public JUTTexture()
        { }

        public JUTTexture(Stream stream) => Read(stream);

        public JUTTexture(string filepath)
        {
            FileStream fs = new(filepath, FileMode.Open);
            Read(fs);
            fs.Close();
            Name = filepath;
        }

        public virtual void Save(string filepath)
        {
            FileStream fs = new(filepath, FileMode.Create);
            Write(fs);
            fs.Close();
            Name = filepath;
        }

        public virtual void Save(Stream stream) => Write(stream);

        public virtual void Open(Stream stream) => Read(stream);

        protected abstract void Read(Stream stream);

        protected abstract void Write(Stream stream);

        public bool ImageEquals(JUTTexture entry) => ListEx.Equals(this, entry);

        public override bool Equals(object obj) => obj is JUTTexture tex && tex.Name.Equals(Name) && ImageEquals(tex);

        public static bool operator ==(JUTTexture texture1, JUTTexture texture2) => texture1.Equals(texture2);

        public static bool operator !=(JUTTexture texture1, JUTTexture texture2) => !(texture1 == texture2);

        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Name);

        #region Dispose

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var item in this)
                        item.Dispose();
                }
                disposedValue = true;
            }
        }

        ~JUTTexture()
        {
            Dispose(disposing: true);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion Dispose
    }
}
