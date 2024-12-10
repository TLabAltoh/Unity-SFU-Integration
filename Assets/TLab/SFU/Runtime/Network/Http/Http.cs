using System.Text;
using UnityEngine;

namespace TLab.SFU.Network
{
    public static class Http
    {
        public static System.Threading.Tasks.Task<string> GetResponse(string url)
        {
            return System.Threading.Tasks.Task.Run(async () =>
            {
                System.Uri uri = new System.UriBuilder(url).Uri;

                var client = new System.Net.Http.HttpClient();
                var res = await client.PostAsync(uri, null);
                res.EnsureSuccessStatusCode();

                string data = await res.Content.ReadAsStringAsync();
                return data;
            });
        }

        public static string GetBase64(string text)
        {
            Debug.Log($"{nameof(GetBase64)}: " + text);

            var base64 = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

            return base64;
        }
    }
}
