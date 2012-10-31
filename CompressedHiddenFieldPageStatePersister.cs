using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Web.UI;

namespace ViewStateCompressor
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class CompressedHiddenFieldPageStatePersister : HiddenFieldPageStatePersister
    {
        public CompressedHiddenFieldPageStatePersister(Page page) : base(page) { }

        public override void Load()
        {
            // depend on HiddenFieldPageStatePersister for heavy lifting and crypto
            base.Load();

            CompressedSerializedData compressedData = ViewState as CompressedSerializedData;
            if (compressedData == null && ControlState != null)
            {
                // the underlying data was not compressed
                return;
            }

            // decompress
            using (MemoryStream uncompressedStream = new MemoryStream())
            {
                using (GZipStream zipStream = new GZipStream(uncompressedStream, CompressionMode.Decompress, leaveOpen: true))
                {
                    zipStream.Write(compressedData.RawData, 0, compressedData.RawData.Length);
                }

                uncompressedStream.Position = 0;
                ObjectStateFormatter formatter = new ObjectStateFormatter();
                Pair pair = (Pair)formatter.Deserialize(uncompressedStream);

                // extract
                ViewState = pair.First;
                ControlState = pair.Second;
            }
        }

        public override void Save()
        {
            using (MemoryStream uncompressedStream = new MemoryStream())
            {
                ObjectStateFormatter formatter = new ObjectStateFormatter();
                formatter.Serialize(uncompressedStream, new Pair(ViewState, ControlState));

                using (MemoryStream compressedStream = new MemoryStream())
                {
                    using (GZipStream zipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        zipStream.Write(uncompressedStream.GetBuffer(), 0, checked((int)uncompressedStream.Length));
                    }

                    if (uncompressedStream.Length > compressedStream.Length)
                    {
                        // compressing will probably save space
                        // CompressedSerializeData uses BinaryFormatter, which ObjectStateFormatter serializes better than byte[]
                        ViewState = new CompressedSerializedData() { RawData = compressedStream.ToArray() };
                        ControlState = null;
                    }

                    // depend on HiddenFieldPageStatePersister for heavy lifting and crypto
                    base.Save();
                }
            }
        }

        [Serializable]
        private sealed class CompressedSerializedData
        {
            public byte[] RawData;
        }
    }
}
