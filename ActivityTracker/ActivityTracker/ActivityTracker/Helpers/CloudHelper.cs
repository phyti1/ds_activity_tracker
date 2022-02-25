using System;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace ActivityTracker.Helpers
{
    public static class CloudHelper
    {
        private static readonly string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=activityprofiles;AccountKey=XrOsi4EjPYpGmVZ5pWK+UNUBSkM2UA13RA8ANFFe89RQ7TAhK3NPaG/SYf1XtkhFEAr5v7stMW4ArXzD4DwBdQ==;EndpointSuffix=core.windows.net";
        private static readonly string ContainerName = "app";

        private static BlobUploadOptions _uploadOptions = new BlobUploadOptions() { HttpHeaders = new BlobHttpHeaders() { ContentEncoding = "gzip", ContentType = "text/csv" } };

        public static async Task<bool> UploadCSV(string content, string fileName)
        {
            try
            {
                var blobClient = new BlobClient(ConnectionString, ContainerName, fileName);
                await blobClient.UploadAsync(Zip(content), _uploadOptions);
                return true;
            }
            catch (RequestFailedException)
            {
                return false;
            }
        }

        private static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        private static MemoryStream Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            var msi = new MemoryStream(bytes);
            var mso = new MemoryStream();
            using (var gs = new GZipStream(mso, CompressionMode.Compress))
            {
                CopyTo(msi, gs);
            }

            return mso;
        }
    }
}
