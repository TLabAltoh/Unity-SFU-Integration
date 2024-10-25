using UnityEditor;
using UnityEngine;
using TLab.SFU.Network;

namespace TLab.SFU.Test.Editor
{
    [CustomEditor(typeof(TestCode))]
    public class TestCodeEditor : UnityEditor.Editor
    {
        [System.Serializable]
        public class TestPacket : Packetable
        {
            public static new int pktId;

            protected override int packetId => pktId;

            static TestPacket() => pktId = MD5From(nameof(TestPacket));

            public Address32[] address;
        }

        private TestCode instance;

        private void OnEnable()
        {
            instance = target as TestCode;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("PacketId Test"))
            {
                Debug.Log($"Packetable: {Packetable.pktId}");
                Debug.Log($"TestPacket: {TestPacket.pktId}");
            }

            if (GUILayout.Button("Address Test"))
            {
                var send = new TestPacket();
                send.address = new Address32[]
                {
                    new Address32(0, 0, 0, 0),
                    new Address32(0, 1, 0, 1),
                    new Address32(1, 0, 1, 1),
                };

                var bytes = send.Marshall();
                Debug.Log($"Befor: {bytes}, Length: {bytes.Length}");
                bytes = UnsafeUtility.Padding(1 + sizeof(int), bytes);
                Debug.Log($"After: {bytes}, Length: {bytes.Length}");

                var receive = new TestPacket();
                receive.UnMarshall(bytes);

                Debug.Log(receive);

                Debug.Log(receive.address[0].Equals(send.address[0]));
                Debug.Log(receive.address[1].Equals(send.address[1]));
                Debug.Log(receive.address[2].Equals(send.address[1]));
                send.address[1].Copy(receive.address[2]);
                Debug.Log(receive.address[2].Equals(send.address[1]));
            }
        }
    }
}
