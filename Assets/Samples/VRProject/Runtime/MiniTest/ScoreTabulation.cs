using System.Collections.Generic;
using System;
using UnityEngine;
using TLab.SFU.Network;

namespace TLab.VRProjct
{
    public class ScoreTabulation : MonoBehaviour, INetworkEventHandler, INetworkClientEventHandler
    {
        private Dictionary<int, int> m_scores = new Dictionary<int, int>();

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public static ScoreTabulation instance;

        #region MESSAGE

        [Serializable, Message(typeof(MSG_MiniTest))]
        public class MSG_MiniTest : Message
        {
            public MSG_MiniTest(int score) : base()
            {
                this.score = score;
            }

            public MSG_MiniTest(byte[] bytes) : base(bytes) { }

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
            m_scores[NetworkClient.userId] = score;

            NetworkClient.SendWS(new MSG_MiniTest(score).Marshall());
        }

        void Awake()
        {
            instance = this;

            NetworkClient.RegisterOnMessage<MSG_MiniTest>(OnMessage);
        }

        public void OnMessage(int from, int to, byte[] bytes)
        {
            var @object = new MSG_MiniTest(bytes);
            m_scores[from] = @object.score;
        }

        public void OnOpen()
        {
            throw new NotImplementedException();
        }

        public void OnClose()
        {
            throw new NotImplementedException();
        }

        public void OnOpen(int from)
        {
            throw new NotImplementedException();
        }

        public void OnClose(int from)
        {
            throw new NotImplementedException();
        }

        public void OnError()
        {
            throw new NotImplementedException();
        }

        public void OnJoin()
        {
            throw new NotImplementedException();
        }

        public void OnExit()
        {
            throw new NotImplementedException();
        }

        public void OnJoin(int userId)
        {
            throw new NotImplementedException();
        }

        public void OnExit(int userId)
        {
            throw new NotImplementedException();
        }
    }
}