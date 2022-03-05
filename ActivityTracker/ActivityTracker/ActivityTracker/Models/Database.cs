using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ActivityTracker.Models
{
    class Database
    {
        public static async Task<bool> SendData(string data)
        {
            //#if DEBUG
            //            return true;
            //#endif

            var filename = $"{Configuration.Instance.Name}_{Configuration.Instance.ActivityType}_{Configuration.Instance.MeasGuid}_{Configuration.Instance.MeasIndex}";
            Configuration.Instance.MeasIndex += 1;
            var _url = $"https://activityprofiles.blob.core.windows.net/app/{filename}?sv=2020-08-04&ss=bf&srt=o&sp=rwdlacitfx&se=2022-09-01T03:14:13Z&st=2022-02-27T20:14:13Z&spr=https&sig=XbB7tjMVVuPbzjih3YX6Kj4ugoWBNfW836NG%2Bz5RyQA%3D";
            var _response = await PutContent(_url, data);
            if(_response == HttpStatusCode.Created)
            {
                return true;
            }
            return false;
        }

        private static async Task<HttpStatusCode> PutContent(string url, string data)
        {

            HttpClient _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("x-ms-blob-type", "BlockBlob");
            _client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "text/csv");

            string _responseString = null;
            HttpContent _content = new StringContent(data);
            HttpResponseMessage _response = null;
            try
            {
                _response = await _client.PutAsync(url, _content).ConfigureAwait(false);
                _responseString = await _response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return _response.StatusCode;
        }


    }
}
