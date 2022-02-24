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
            var _url = $"http://192.168.0.1/post";
            var _response = await PostContent(_url, data);
            if(_response == "ok")
            {
                return true;
            }
            return false;
        }

        private static async Task<string> PostContent(string url, string data)
        {
            HttpClient _client = new HttpClient();
            string _responseString = null;
            FormUrlEncodedContent urlParams = null;
            HttpContent _content = new StringContent(data);
            HttpResponseMessage _response = null;
            try
            {
                _response = await _client.PostAsync(url + urlParams, _content).ConfigureAwait(false);
                _responseString = await _response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return _responseString;
        }


    }
}
