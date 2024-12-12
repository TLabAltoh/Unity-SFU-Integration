using System.Collections;
using System;
using System.Linq;
using UnityEngine;

namespace TLab.SFU.Network
{
    using Registry = Registry<Address64, NetworkAnimator>;

    [AddComponentMenu("TLab/SFU/Network Animator (TLab)")]
    public class NetworkAnimator : NetworkObject
    {
        #region STRUCT

        [Serializable]
        public class AnimatorControllerParameterHistory
        {
            public AnimatorControllerParameterType type;
            public string name;
            public int lastValueHash;
        }

        [Serializable]
        public struct AnimatorControllerParameterState
        {
            public Address64 id;
            public string parameter;
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

        private static MSG_SyncAnimatorController packetBuf = new MSG_SyncAnimatorController(new Address64(), null);

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        protected virtual void SyncAnimatorController(int to, bool requested = false, params AnimatorControllerParameterHistory[] parameters)
        {
            var parameterStates = new AnimatorControllerParameterState[parameters.Length];

            for (int i = 0; i < parameterStates.Length; i++)
            {
                var parameter = parameters[i];

                var animState = new AnimatorControllerParameterState
                {
                    id = m_networkId.id,
                    parameter = parameter.name
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

            packetBuf.networkId = m_networkId.id;
            packetBuf.requested = requested;
            packetBuf.parameterStates = parameterStates;

            NetworkClient.instance.SendWS(to, packetBuf.Marshall());

            m_synchronised = false;
        }

        protected virtual void SyncAnimatorController(int to, bool force = false, bool requested = false)
        {
            foreach (AnimatorControllerParameterHistory parameter in m_parameters.Values)
            {
                int prevValueHash = parameter.lastValueHash;
                int currentValueHash;

                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Int:
                        currentValueHash = m_animator.GetInteger(parameter.name).GetHashCode();
                        parameter.lastValueHash = currentValueHash;
                        break;
                    case AnimatorControllerParameterType.Float:
                        currentValueHash = m_animator.GetFloat(parameter.name).GetHashCode();
                        parameter.lastValueHash = currentValueHash;
                        break;
                    case AnimatorControllerParameterType.Bool:
                        currentValueHash = m_animator.GetBool(parameter.name).GetHashCode();
                        parameter.lastValueHash = currentValueHash;
                        break;
                    default:    // case AnimatorControllerParameterType.Trigger:
                        currentValueHash = m_animator.GetBool(parameter.name).GetHashCode();
                        parameter.lastValueHash = currentValueHash;
                        break;
                }

                if (force || (prevValueHash != currentValueHash))
                    SyncAnimatorController(to, requested, parameter);
            }
        }

        public override void OnSyncRequested(int from)
        {
            base.OnSyncRequested(from);

            SyncAnimatorController(from, true, m_parameters.Values.Cast<AnimatorControllerParameterHistory>().ToArray());
        }

        public void SyncFrom(int from, in AnimatorControllerParameterState[] parameterStates)
        {
            foreach (var animState in parameterStates)
            {
                switch (animState.type)
                {
                    case (int)AnimatorControllerParameterValueType.Float:
                        if (ApplyParameter(animState.parameter, animState.f.GetHashCode()))
                            SetFloat(animState.parameter, animState.f);
                        break;
                    case (int)AnimatorControllerParameterValueType.Int:
                        if (ApplyParameter(animState.parameter, animState.i.GetHashCode()))
                            SetInteger(animState.parameter, animState.i);
                        break;
                    case (int)AnimatorControllerParameterValueType.Bool:
                        if (ApplyParameter(animState.parameter, animState.z.GetHashCode()))
                            SetBool(animState.parameter, animState.z);
                        break;
                    case (int)AnimatorControllerParameterValueType.Trigger:
                        if (ApplyParameter(animState.parameter, animState.z.GetHashCode()))
                            SetTrigger(animState.parameter, animState.z);
                        break;
                }
            }

            m_synchronised = true;
        }

        public override void SyncViaWebRTC(int to, bool force = false, bool requested = false) => SyncAnimatorController(to, force, requested);

        protected virtual bool ApplyParameter(string paramName, int hashCode)
        {
            var parameterInfo = m_parameters[paramName] as AnimatorControllerParameterHistory;

            if (parameterInfo == null)
            {
                Debug.LogError("Animation Parameter Not Found:" + paramName);
                return false;
            }

            var dirty = parameterInfo.lastValueHash != hashCode;

            parameterInfo.lastValueHash = hashCode;

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
                var parameterInfo = new AnimatorControllerParameterHistory();
                var parameter = m_animator.GetParameter(i);
                parameterInfo.type = parameter.type;
                parameterInfo.name = parameter.name;

                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Int:
                        parameterInfo.lastValueHash = m_animator.GetInteger(parameterInfo.name).GetHashCode();
                        break;
                    case AnimatorControllerParameterType.Float:
                        parameterInfo.lastValueHash = m_animator.GetFloat(parameterInfo.name).GetHashCode();
                        break;
                    case AnimatorControllerParameterType.Bool:
                        parameterInfo.lastValueHash = m_animator.GetBool(parameterInfo.name).GetHashCode();
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        parameterInfo.lastValueHash = m_animator.GetBool(parameterInfo.name).GetHashCode();
                        break;
                }

                m_parameters[parameterInfo.name] = parameterInfo;
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
                packetBuf.UnMarshall(bytes);
                var animator = Registry.GetByKey(packetBuf.networkId);
                if (animator)
                {
                    animator.SyncFrom(from, packetBuf.parameterStates);
                    if (packetBuf.requested)
                        animator.OnSyncRequestCompleted(from);
                }
            });
        }

        protected override void Register()
        {
            base.Register();

            Registry.Register(m_networkId.id, this);
        }

        protected override void UnRegister()
        {
            Registry.UnRegister(m_networkId.id);

            base.UnRegister();
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
    }
}
