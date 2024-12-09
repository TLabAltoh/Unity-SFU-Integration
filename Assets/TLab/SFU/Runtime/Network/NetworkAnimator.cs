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
        public class AnimParameter
        {
            public AnimatorControllerParameterType type;
            public string name;
            public int lastValueHash;
        }

        [Serializable]
        public struct WebAnimState
        {
            public Address64 id;
            public string parameter;
            public int type;

            public float floatVal;
            public int intVal;
            public bool boolVal;
        }

        public enum WebAnimValueType
        {
            Float,
            Int,
            Bool,
            Trigger,
        }

        #endregion STRUCT

        #region MESSAGE

        [Serializable, Message(typeof(MSG_SyncAnim))]
        public class MSG_SyncAnim : MSG_Sync
        {
            public Address64 networkId;
            public WebAnimState[] animStates;

            public MSG_SyncAnim(Address64 networkId, WebAnimState[] animStates) : base()
            {
                this.networkId = networkId;
                this.animStates = animStates;
            }

            public MSG_SyncAnim(byte[] bytes) : base(bytes) { }
        }

        #endregion MESSAGE

        [SerializeField] private Animator m_animator;

        private Hashtable m_parameters = new Hashtable();

        private MSG_SyncAnim m_tmp = new MSG_SyncAnim(new Address64(), null);

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        protected virtual void SyncAnim(int to, bool requested = false, params AnimParameter[] parameters)
        {
            var animStates = new WebAnimState[parameters.Length];

            for (int i = 0; i < animStates.Length; i++)
            {
                var parameter = parameters[i];

                var animState = new WebAnimState
                {
                    id = m_networkId.id,
                    parameter = parameter.name
                };

                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Int:
                        animState.type = (int)WebAnimValueType.Int;
                        animState.intVal = m_animator.GetInteger(parameter.name);
                        break;
                    case AnimatorControllerParameterType.Float:
                        animState.type = (int)WebAnimValueType.Float;
                        animState.floatVal = m_animator.GetFloat(parameter.name);
                        break;
                    case AnimatorControllerParameterType.Bool:
                        animState.type = (int)WebAnimValueType.Bool;
                        animState.boolVal = m_animator.GetBool(parameter.name);
                        break;
                    default: // AnimatorControllerParameterType.Trigger:
                        animState.type = (int)WebAnimValueType.Trigger;
                        animState.boolVal = m_animator.GetBool(parameter.name);
                        break;
                }

                animStates[i] = animState;
            }

            m_tmp.networkId = m_networkId.id;
            m_tmp.requested = requested;
            m_tmp.animStates = animStates;

            NetworkClient.instance.SendWS(to, m_tmp.Marshall());

            m_synchronised = false;
        }

        protected virtual void SyncAnim(int to, bool force = false, bool requested = false)
        {
            foreach (AnimParameter parameter in m_parameters.Values)
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
                    SyncAnim(to, requested, parameter);
            }
        }

        public override void OnSyncRequested(int from)
        {
            base.OnSyncRequested(from);

            SyncAnim(from, true, m_parameters.Values.Cast<AnimParameter>().ToArray());
        }

        public void SyncFrom(int from, WebAnimState[] animStates)
        {
            foreach (var animState in animStates)
            {
                switch (animState.type)
                {
                    case (int)WebAnimValueType.Float:
                        if (ApplyParameter(animState.parameter, animState.floatVal.GetHashCode()))
                            SetFloat(animState.parameter, animState.floatVal);
                        break;
                    case (int)WebAnimValueType.Int:
                        if (ApplyParameter(animState.parameter, animState.intVal.GetHashCode()))
                            SetInteger(animState.parameter, animState.intVal);
                        break;
                    case (int)WebAnimValueType.Bool:
                        if (ApplyParameter(animState.parameter, animState.boolVal.GetHashCode()))
                            SetBool(animState.parameter, animState.boolVal);
                        break;
                    case (int)WebAnimValueType.Trigger:
                        if (ApplyParameter(animState.parameter, animState.boolVal.GetHashCode()))
                            SetTrigger(animState.parameter, animState.boolVal);
                        break;
                }
            }

            m_synchronised = true;
        }

        public override void SyncViaWebRTC(int to, bool force = false, bool requested = false) => SyncAnim(to, force, requested);

        protected virtual bool ApplyParameter(string paramName, int hashCode)
        {
            var parameterInfo = m_parameters[paramName] as AnimParameter;

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

        protected virtual void InitAnimationParameter()
        {
            int parameterLength = m_animator.parameters.Length;
            for (int i = 0; i < parameterLength; i++)
            {
                var parameterInfo = new AnimParameter();
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

        public override void Init(Address32 id, bool self)
        {
            base.Init(id, self);

            InitAnimationParameter();
        }

        public override void Init(bool self)
        {
            base.Init(self);

            InitAnimationParameter();
        }

        protected override void RegisterOnMessage()
        {
            base.RegisterOnMessage();

            NetworkClient.RegisterOnMessage<MSG_SyncAnim>((from, to, bytes) =>
            {
                m_tmp.UnMarshall(bytes);
                var animator = Registry.GetByKey(m_tmp.networkId);
                if (animator)
                {
                    animator.SyncFrom(from, m_tmp.animStates);
                    if (m_tmp.requested)
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
