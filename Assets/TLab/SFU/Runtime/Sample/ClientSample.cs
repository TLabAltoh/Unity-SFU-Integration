using System.Collections;
using UnityEngine;
using TLab.SFU.UI;
using TLab.SFU.Network;

namespace TLab.SFU.Sample
{
    public class ClientSample : MonoBehaviour
    {
        [SerializeField] protected LogView m_logView;

        protected Adapter m_adapter;

        protected const string STREAM = "defualt";

        private string THIS_NAME => "[" + this.GetType() + "] ";

        public virtual void Open() { }

        public virtual void Close() { }

        public virtual void SendText(string message) { }

        public virtual void Send(byte[] bytes) { }

        private void OnJoin(string response)
        {
            m_logView?.Append("Response: " + response);
            Open();
        }

        public void Join()
        {
            if (AdapterSample.local && (m_adapter == null))
            {
                Debug.LogError(THIS_NAME + "Adapter is NULL");
                return;
            }

            m_adapter = AdapterSample.instance.GetClone();

            m_adapter.Join(this, OnJoin);
        }

        public void Exit()
        {
            if (m_adapter == null)
            {
                Debug.LogError(THIS_NAME + "Adapter is NULL");
                return;
            }

            Close();

            m_adapter.Exit(this, (response) => m_logView?.Append("Response: " + response));
        }

        private IEnumerator CloneAdapter()
        {
            while (AdapterSample.state != AdapterSample.State.Created)
                yield return null;

            m_adapter = AdapterSample.instance.GetClone();
        }

        protected virtual void Start()
        {
            if (AdapterSample.local)
                StartCoroutine(CloneAdapter());
        }
    }
}
