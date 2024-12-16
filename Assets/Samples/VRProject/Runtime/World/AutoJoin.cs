using UnityEngine;
using UnityEngine.Events;
using TLab.SFU.Network;
using TLab.SFU.Network.Json;

namespace TLab.VRProjct
{
    public class AutoJoin : MonoBehaviour
    {
        [SerializeField] private UnityEvent<string> m_onLog;

        private void OnJoin(string @string)
        {
            m_onLog.Invoke(@string);
            NetworkClient.ConnectAsync();
        }

        private void OnCreate(string @string) => NetworkClient.adapter.Join(this, OnJoin);

        public void Join() => NetworkClient.adapter.GetInfo(this, (@string) =>
        {
            var adapter = NetworkClient.adapter;

            m_onLog.Invoke(@string);

            var response = new RoomInfos(@string);

            if (response.infos.Length == 0)
                adapter.Create(this, OnCreate);
            else
            {
                m_onLog.Invoke(@string);
                adapter.Init(adapter.config, response.infos[0].id, adapter.sharedKey, adapter.masterKey);
                adapter.Join(this, OnJoin);
            }
        });

        private void OnExit(string @string) => m_onLog.Invoke(@string);

        public void Exit()
        {
            NetworkClient.HangUpAll();

            NetworkClient.adapter.Exit(this, OnExit);
        }

        private void Start() => Join();
    }
}
