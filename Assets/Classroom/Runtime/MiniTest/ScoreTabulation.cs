using System.Collections.Generic;
using UnityEngine;
using TLab.SFU.Network;

namespace TLab.VRClassroom
{
    public class ScoreTabulation : MonoBehaviour, INetworkEventHandler, ISyncEventHandler
    {
        private Dictionary<int, int> m_scores = new Dictionary<int, int>();

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public static ScoreTabulation instance;

        #region MESSAGE_TYPE

        [System.Serializable]
        public struct MSG_MiniTest : Packetable
        {
            public static int pktId;

            static MSG_MiniTest() => pktId = nameof(MSG_MiniTest).GetHashCode();

            public int score;

            public byte[] Marshall() => Packetable.MarshallJson(pktId, this);

            public void UnMarshall(byte[] bytes) => Packetable.UnMarshallJson(bytes, this);
        }

        #endregion MESSAGE_TYPE

        public int GetScore(int index)
        {
            if (m_scores.ContainsKey(index))
            {
                return m_scores[index];
            }

            return 0;
        }

        public void RegistScore(int score = 0)
        {
            m_scores[SyncClient.userId] = score;

            var @object = new MSG_MiniTest
            {
                score = score,
            };

            SyncClient.instance.SendWS(@object.Marshall());
        }

        void Awake()
        {
            instance = this;

            SyncClient.RegisterOnMessage(MSG_MiniTest.pktId, OnMessage);
        }

        public void OnMessage(int from, int to, byte[] bytes)
        {
            var @object = new MSG_MiniTest();
            @object.UnMarshall(bytes);
            m_scores[from] = @object.score;
        }

        public void OnOpen()
        {
            throw new System.NotImplementedException();
        }

        public void OnClose()
        {
            throw new System.NotImplementedException();
        }

        public void OnOpen(int from)
        {
            throw new System.NotImplementedException();
        }

        public void OnClose(int from)
        {
            throw new System.NotImplementedException();
        }

        public void OnJoin()
        {
            throw new System.NotImplementedException();
        }

        public void OnExit()
        {
            throw new System.NotImplementedException();
        }

        public void OnJoin(int userId)
        {
            throw new System.NotImplementedException();
        }

        public void OnExit(int userId)
        {
            throw new System.NotImplementedException();
        }
    }
}