using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.SFU.Network.Test
{
    public class AddressTest : MonoBehaviour
    {
        [System.Serializable]
        public struct TestData
        {
            public Address32[] address;
        }

        void Start()
        {
            var send = new TestData();
            send.address = new Address32[]
            {
                new Address32(0, 0, 0, 0),
                new Address32(0, 1, 0, 1),
                new Address32(1, 0, 1, 1),
            };

            var json = JsonUtility.ToJson(send);
            Debug.Log(json);

            var receive = JsonUtility.FromJson<TestData>(json);
            Debug.Log(receive.address[0].Equals(send.address[0]));
            Debug.Log(receive.address[1].Equals(send.address[1]));
            Debug.Log(receive.address[2].Equals(send.address[1]));
            send.address[1].Copy(receive.address[2]);
            Debug.Log(receive.address[2].Equals(send.address[1]));
        }
    }
}
