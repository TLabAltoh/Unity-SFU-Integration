#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine;
using TLab.SFU.Network;

#if UNITY_EDITOR
namespace TLab.SFU.Test.Editor
{
    [CustomEditor(typeof(TestCode))]
    public class TestCodeEditor : UnityEditor.Editor
    {
        [Serializable, Message(typeof(TestMessage))]
        public class TestMessage : Message
        {
            public TestMessage() : base() { }

            public TestMessage(byte[] bytes) : base(bytes) { }

            public Address32[] address;
        }

        private TestCode instance;

        private void OnEnable() => instance = target as TestCode;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("MsgId Test"))
                Debug.Log($"Message.GetMsgId<TestMessage>(): {Message.GetMsgId<TestMessage>()}");

            if (GUILayout.Button("Address Test"))
            {
                var send = new TestMessage();
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

                var receive = new TestMessage(bytes);
                //receive.UnMarshall(bytes);    // <-- Test for a call to Message.Cache() or not in JsonUtility.FromJsonOverwrite()

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
#endif