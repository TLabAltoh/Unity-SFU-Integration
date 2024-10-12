using System.Collections;
using System.Text;
using UnityEngine;
using NativeWebSocket;

namespace TLab.SFU.Network.Test
{
    public class WebSocketTest : MonoBehaviour
    {
        private WebSocket m_socket;

        private Coroutine m_task;

        private class Request
        {
            public int user_id;
            public string room_name;
        }

        private void HangUpTask()
        {
            if (m_task != null)
            {
                StopCoroutine(m_task);
                m_task = null;
            }
        }

        private IEnumerator Start()
        {
            m_socket = new WebSocket("ws://localhost:5000");
            m_socket.OnOpen += () =>
            {
                Debug.Log("Connection open!");
                var request = new Request
                {
                    user_id = 0,
                    room_name = "test",
                };
                var json = JsonUtility.ToJson(request);
                m_socket.SendText(json);
            };
            m_socket.OnError += (e) => Debug.Log("Error! " + e);
            m_socket.OnClose += (e) => Debug.Log("Connection closed!");
            m_socket.OnMessage += (bytes) =>
            {
                var message = Encoding.UTF8.GetString(bytes);
                Debug.Log(message);
            };
            _ = m_socket.Connect();

            while (m_socket.State == WebSocketState.Connecting)
            {
                Debug.Log("Connecting ...");

                yield return new WaitForSeconds(5f);
            }

            yield return new WaitForSeconds(5f);

            Debug.Log("Closing ...");

            _ = m_socket.Close();
        }

        private void OnApplicationQuit()
        {
            HangUpTask();
        }
    }
}
