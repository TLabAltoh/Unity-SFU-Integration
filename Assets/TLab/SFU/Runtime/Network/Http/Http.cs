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

        public static string GetBase64(object @object)
        {
            var json = JsonUtility.ToJson(@object);

            Debug.Log("Json: " + json);

            var base64 = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            return base64;
        }
    }
}
