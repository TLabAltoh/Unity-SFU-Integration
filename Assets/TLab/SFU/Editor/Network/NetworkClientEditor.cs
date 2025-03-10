#define TEST
#undef TEST

using UnityEngine;
using UnityEditor;

#if TEST
using static TLab.SFU.UnsafeUtility;
#endif

namespace TLab.SFU.Network.Editor
{
    [CustomEditor(typeof(NetworkClient))]
    public class NetworkClientEditor : UnityEditor.Editor
    {
        private NetworkClient m_instance;

        private void OnEnable() => m_instance = target as NetworkClient;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

#if TEST
            if (GUILayout.Button("Test"))
            {
                {
                    Debug.Log("MSG_Join: " + NetworkClient.MSG_Join.pktId);

                    var action = new PrefabStore.StoreAction()
                    {
                        action = PrefabStore.StoreAction.Action.INSTANTIATE,
                        elemId = 0,
                        userId = 0,
                        publicId = UniqueId.Generate(),
                        transform = new WebTransform(new WebVector3(0, 0, 0), new WebVector4(1, 0, 0, 0)),
                    };

                    var send = new NetworkClient.MSG_Join()
                    {
                        messageType = 0,
                        avatorAction = action,
                    };

                    var binary = send.Marshall();
                    Debug.Log("Binary: " + binary);

                    binary = Padding(5, binary);  // 5 = recv packet hedder lenght - send packet hedder length

                    var recv = new NetworkClient.MSG_Join();
                    recv.UnMarshall(binary);

                    Debug.Log("Equals messageType: " + recv.messageType.Equals(send.messageType));
                    Debug.Log("Equals avatorAction: " + recv.avatorAction.Equals(send.avatorAction));
                }

                {
                    Debug.Log("MSG_IdAvails: " + NetworkClient.MSG_IdAvails.pktId);

                    var send = new NetworkClient.MSG_IdAvails();
                    send.length = 5;

                    var binary = send.Marshall();
                    binary = Padding(5, binary);

                    var res = new NetworkClient.MSG_IdAvails();
                    res.UnMarshall(binary);
                    res.idAvails = UniqueId.Generate(res.length);

                    Debug.Log("Length: " + res.idAvails.Length);

                    binary = res.Marshall();
                    binary = Padding(5, binary);

                    var result = new NetworkClient.MSG_IdAvails();
                    result.UnMarshall(binary);

                    Debug.Log("Length: " + result.length);
                    Debug.Log("Lenght: " + result.idAvails.Length);
                }
            }
#endif

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                GUILayout.Label($"My user id: {NetworkClient.userId}", GUILayout.ExpandWidth(false));
                EditorGUILayout.Space();
            }
        }
    }
}
