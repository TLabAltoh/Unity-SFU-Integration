using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using UnityEngine;

namespace TLab.SFU.Network
{
    // TODO: 
    // 1. Add interpolation

    using Registry = Registry<Address64, NetworkAnimator>;

    [AddComponentMenu("TLab/SFU/Network Animator (TLab)")]
    public class NetworkAnimator : NetworkObject
    {
        #region STRUCT

        [Serializable]
        public struct AnimatorControllerParameterHistory
        {
            public AnimatorControllerParameterType type;
            public string name;

            public int lastValueHash;
        }

        [Serializable]
        public struct AnimatorControllerParameterState
        {
            public Address64 id;
            public string name;
            public int type;

            public float f;
            public int i;
            public bool z;
        }

        public enum AnimatorControllerParameterValueType
        {
            Float,
            Int,
            Bool,
            Trigger,
        }

        #endregion STRUCT

        #region MESSAGE

        [Serializable, Message(typeof(MSG_SyncAnimatorController))]
        public class MSG_SyncAnimatorController : MSG_Sync
        {
            public Address64 networkId;
            public AnimatorControllerParameterState[] parameterStates;

            public MSG_SyncAnimatorController(Address64 networkId, AnimatorControllerParameterState[] parameterStates) : base()
            {
                this.networkId = networkId;
                this.parameterStates = parameterStates;
            }

            public MSG_SyncAnimatorController(byte[] bytes) : base(bytes) { }
        }

        #endregion MESSAGE

        [SerializeField] private Animator m_animator;

        private Hashtable m_parameters = new Hashtable();

        private static MSG_SyncAnimatorController m_packet = new MSG_SyncAnimatorController(new Address64(), null);

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        protected AnimatorControllerParameterState[] GetAnimatorControllerParameterStateArray(params AnimatorControllerParameterHistory[] parameters)
        {
            var parameterStates = new AnimatorControllerParameterState[parameters.Length];

            for (int i = 0; i < parameterStates.Length; i++)
            {
                var parameter = parameters[i];

                var animState = new AnimatorControllerParameterState
                {
                    id = m_networkId.id,
                    name = parameter.name
                };

                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Int:
                        animState.type = (int)AnimatorControllerParameterValueType.Int;
                        animState.i = m_animator.GetInteger(parameter.name);
                        break;
                    case AnimatorControllerParameterType.Float:
                        animState.type = (int)AnimatorControllerParameterValueType.Float;
                        animState.f = m_animator.GetFloat(parameter.name);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        animState.type = (int)AnimatorControllerParameterValueType.Bool;
                        animState.z = m_animator.GetBool(parameter.name);
                        break;
                    default: // AnimatorControllerParameterType.Trigger:
                        animState.type = (int)AnimatorControllerParameterValueType.Trigger;
                        animState.z = m_animator.GetBool(parameter.name);
                        break;
                }

                parameterStates[i] = animState;
            }

            return parameterStates;
        }

        protected virtual bool ApplyCurrentAnimatorController(out AnimatorControllerParameterHistory[] updatedParameters)
        {
            var updatedParameterQueue = new Queue<AnimatorControllerParameterHistory>();

            foreach (AnimatorControllerParameterHistory parameter in m_parameters.Values)
            {
                int prevValueHash = parameter.lastValueHash;
                int currentValueHash;

                var updatedParameter = parameter;

                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Int:
                        currentValueHash = m_animator.GetInteger(parameter.name).GetHashCode();
                        updatedParameter.lastValueHash = currentValueHash;
                        break;
                    case AnimatorControllerParameterType.Float:
                        currentValueHash = m_animator.GetFloat(parameter.name).GetHashCode();
                        updatedParameter.lastValueHash = currentValueHash;
                        break;
                    case AnimatorControllerParameterType.Bool:
                        currentValueHash = m_animator.GetBool(parameter.name).GetHashCode();
                        updatedParameter.lastValueHash = currentValueHash;
                        break;
                    default:    // case AnimatorControllerParameterType.Trigger:
                        currentValueHash = m_animator.GetBool(parameter.name).GetHashCode();
                        updatedParameter.lastValueHash = currentValueHash;
                        break;
                }

                m_parameters[parameter.name] = updatedParameter;

                if (prevValueHash != currentValueHash)
                    updatedParameterQueue.Enqueue(parameter);
            }

            updatedParameters = updatedParameterQueue.ToArray();

            return updatedParameterQueue.Count > 0;
        }

        public override void OnSyncRequest(int from)
        {
            base.OnSyncRequest(from);

            SyncViaWebSocket(from, true, true);
        }

        public void SyncFrom(int from, bool immediate, in AnimatorControllerParameterState[] parameterStates)
        {
            foreach (var animState in parameterStates)
            {
                switch (animState.type)
                {
                    case (int)AnimatorControllerParameterValueType.Float:
                        if (ApplyParameter(animState.name, animState.f.GetHashCode()))
                            SetFloat(animState.name, animState.f);
                        break;
                    case (int)AnimatorControllerParameterValueType.Int:
                        if (ApplyParameter(animState.name, animState.i.GetHashCode()))
                            SetInteger(animState.name, animState.i);
                        break;
                    case (int)AnimatorControllerParameterValueType.Bool:
                        if (ApplyParameter(animState.name, animState.z.GetHashCode()))
                            SetBool(animState.name, animState.z);
                        break;
                    case (int)AnimatorControllerParameterValueType.Trigger:
                        if (ApplyParameter(animState.name, animState.z.GetHashCode()))
                            SetTrigger(animState.name, animState.z);
                        break;
                }
            }

            m_synchronised = true;
        }

        private void UpdateSendPacket(AnimatorControllerParameterState[] parameterStates, bool request = false, bool immediate = false)
        {
            m_packet.networkId = m_networkId.id;
            m_packet.request = request;
            m_packet.immediate = immediate;
            m_packet.parameterStates = parameterStates;
        }

        private void SendRTC(int to, AnimatorControllerParameterState[] parameterStates, bool request = false, bool immediate = false)
        {
            UpdateSendPacket(parameterStates, request, immediate);
            NetworkClient.SendRTC(to, m_packet.Marshall());

            m_synchronised = false;
        }

        private void SendWS(int to, AnimatorControllerParameterState[] parameterStates, bool request = false, bool immediate = false)
        {
            UpdateSendPacket(parameterStates, request, immediate);
            NetworkClient.SendWS(to, m_packet.Marshall());

            m_synchronised = false;
        }

        public override void SyncViaWebRTC(int to, bool force = false, bool request = false, bool immediate = false)
        {
            if (force)
            {
                var parameterHistrys = m_parameters.Values.Cast<AnimatorControllerParameterHistory>().ToArray();
                var parameterStates = GetAnimatorControllerParameterStateArray(parameterHistrys);

                SendRTC(to, parameterStates, request, immediate);

                return;
            }

            if (ApplyCurrentAnimatorController(out var updatedParameters))
                SendRTC(to, GetAnimatorControllerParameterStateArray(updatedParameters), request, immediate);
        }

        public override void SyncViaWebSocket(int to, bool force = false, bool request = false, bool immediate = false)
        {
            if (force)
            {
                var parameters = m_parameters.Values.Cast<AnimatorControllerParameterHistory>().ToArray();

                SendWS(to, GetAnimatorControllerParameterStateArray(parameters), request, immediate);

                return;
            }

            if (ApplyCurrentAnimatorController(out var updatedParameters))
                SendWS(to, GetAnimatorControllerParameterStateArray(updatedParameters), request, immediate);
        }

        protected virtual bool ApplyParameter(string paramName, int hashCode)
        {
            var parameterHistry = (AnimatorControllerParameterHistory)m_parameters[paramName];

            var dirty = parameterHistry.lastValueHash != hashCode;

            parameterHistry.lastValueHash = hashCode;

            m_parameters[parameterHistry.name] = parameterHistry;

            return dirty;
        }

        public virtual void SetInteger(string paramName, int value) => m_animator.SetInteger(paramName, value);

        public virtual void SetFloat(string paramName, float value) => m_animator.SetFloat(paramName, value);

        public virtual void SetBool(string paramName, bool value) => m_animator.SetBool(paramName, value);

        public virtual void SetTrigger(string paramName, bool value)
        {
            if (value)
                m_animator.SetTrigger(paramName);
        }

        protected virtual void InitAnimatorControllerParameterHistory()
        {
            int parameterLength = m_animator.parameters.Length;
            for (int i = 0; i < parameterLength; i++)
            {
                var parameterHistry = new AnimatorControllerParameterHistory();
                var parameter = m_animator.GetParameter(i);
                parameterHistry.type = parameter.type;
                parameterHistry.name = parameter.name;

                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Int:
                        parameterHistry.lastValueHash = m_animator.GetInteger(parameterHistry.name).GetHashCode();
                        break;
                    case AnimatorControllerParameterType.Float:
                        parameterHistry.lastValueHash = m_animator.GetFloat(parameterHistry.name).GetHashCode();
                        break;
                    case AnimatorControllerParameterType.Bool:
                        parameterHistry.lastValueHash = m_animator.GetBool(parameterHistry.name).GetHashCode();
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        parameterHistry.lastValueHash = m_animator.GetBool(parameterHistry.name).GetHashCode();
                        break;
                }

                m_parameters[parameterHistry.name] = parameterHistry;
            }
        }

        public override void Init(in Address32 id, bool self)
        {
            base.Init(id, self);

            InitAnimatorControllerParameterHistory();
        }

        public override void Init(bool self)
        {
            base.Init(self);

            InitAnimatorControllerParameterHistory();
        }

        protected override void RegisterOnMessage()
        {
            base.RegisterOnMessage();

            NetworkClient.RegisterOnMessage<MSG_SyncAnimatorController>((from, to, bytes) =>
            {
                m_packet.UnMarshall(bytes);
                var animator = Registry.GetByKey(m_packet.networkId);
                if (animator)
                {
                    animator.SyncFrom(from, m_packet.immediate, m_packet.parameterStates);
                    if (m_packet.request)
                        animator.OnSyncRequestComplete(from);
                }
            });
        }

        protected override void Register()
        {
            base.Register();

            Registry.Register(m_networkId.id, this);
        }

        protected override void Unregister()
        {
            Registry.Unregister(m_networkId.id);

            base.Unregister();
        }

        protected override void Start()
        {
            base.Start();

            m_animator = GetComponent<Animator>();
        }

        protected override void Update()
        {
            base.Update();

            SyncViaWebRTC(NetworkClient.userId);
        }

#if UNITY_EDITOR
        protected virtual void Reset()
        {
            m_syncDefault = SocketType.WebSocket;
        }
#endif
    }
}
