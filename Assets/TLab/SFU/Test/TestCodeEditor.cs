using UnityEditor;
using UnityEngine;
using TLab.SFU.Network;

namespace TLab.SFU.Test.Editor
{
    [CustomEditor(typeof(TestCode))]
    public class TestCodeEditor : UnityEditor.Editor
    {
        [System.Serializable]
        public class TestData : IPacketable
        {
            static TestData()
            {
                pktId = nameof(TestData).GetHashCode();
            }

            public static int pktId;

            public Address32[] address;

            public byte[] Marshall() => IPacketable.MarshallJson(pktId, this);

            public void UnMarshall(byte[] bytes) => IPacketable.UnMarshallJson(bytes, this);
        }

        private TestCode instance;

        private void OnEnable()
        {
            instance = target as TestCode;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Address Test"))
            {
                var send = new TestData();
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

                var receive = new TestData();
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
