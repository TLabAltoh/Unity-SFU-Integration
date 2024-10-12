using System.Collections.Generic;
using UnityEngine;
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
        public class MCH_MiniTest
        {
            public int score;
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

            var json = new MCH_MiniTest
            {
                score = score,
            };
            MasterChannelSend(JsonUtility.ToJson(json));
        }

        public void MasterChannelSend(string message)
        {
            SyncClient.instance.MasterChannelSend(
                messageType: nameof(MCH_MiniTest), message: message);
        }

        void Awake()
        {
            instance = this;

            SyncClient.RegisterMasterChannelCallback(nameof(MCH_MiniTest), (obj) =>
            {
                var json = JsonUtility.FromJson<MCH_MiniTest>(obj.message);

                m_scores[obj.srcIndex] = json.score;
            });
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