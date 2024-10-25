using System.Collections.Generic;
using UnityEngine;
using TLab.SFU.Network;

namespace TLab.VRProjct
{
    public class ScoreTabulation : MonoBehaviour, INetworkEventHandler, ISyncEventHandler
    {
        private Dictionary<int, int> m_scores = new Dictionary<int, int>();

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public static ScoreTabulation instance;

        #region MESSAGE

        [System.Serializable]
        public class MSG_MiniTest : Packetable
        {
            public static new int pktId;

            protected override int packetId => pktId;

            static MSG_MiniTest() => pktId = MD5From(nameof(MSG_MiniTest));

            public int score;
        }

        #endregion MESSAGE

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

        public void OnError()
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