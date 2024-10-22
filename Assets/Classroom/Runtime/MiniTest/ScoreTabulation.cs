using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TLab.SFU;
using TLab.SFU.Network;

namespace TLab.VRClassroom
{
    public class ScoreTabulation : MonoBehaviour, NetworkedEventable
    {
        private Dictionary<int, int> m_scores = new Dictionary<int, int>();

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public static ScoreTabulation instance;

        #region MESSAGE_TYPE

        [System.Serializable]
        public class MCH_MiniTest : Packetable
        {
            public static int pktId;

            static MCH_MiniTest() => pktId = nameof(MCH_MiniTest).GetHashCode();

            public int score;

            public byte[] Marshall()
            {
                var json = JsonUtility.ToJson(this);
                return UnsafeUtility.Combine(pktId, Encoding.UTF8.GetBytes(json));
            }

            public void UnMarshall(byte[] bytes)
            {
                var json = Encoding.UTF8.GetString(bytes, SyncClient.HEADER_SIZE, bytes.Length - SyncClient.HEADER_SIZE);
                JsonUtility.FromJsonOverwrite(json, this);
            }
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

            var @object = new MCH_MiniTest
            {
                score = score,
            };

            SyncClient.instance.MasterChannelSend(@object.Marshall());
        }

        void Awake()
        {
            instance = this;

            SyncClient.RegisterMasterChannelCallback(MCH_MiniTest.pktId, OnReceive);
        }

        public void OnReceive(int from, byte[] bytes)
        {
            var @object = new MCH_MiniTest();
            @object.UnMarshall(bytes);
            m_scores[from] = @object.score;
        }

        public void OnOthersJoined(int userId)
        {
            // TODO:
        }

        public void OnOthersExited(int userId)
        {
            // TODO:
        }
    }
}