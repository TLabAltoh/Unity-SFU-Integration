using System.Collections;
using UnityEngine;

namespace TLab.SFU.Network
{
    [AddComponentMenu("TLab/SFU/Sync Animator (TLab)")]
    public class SyncAnimator : NetworkedObject
    {
        #region STRUCT

        [System.Serializable]
        public class AnimParameter
        {
            public AnimatorControllerParameterType type;
            public string name;
            public int lastValueHash;
        }

        [System.Serializable]
        public struct WebAnimState
        {
            public Address64 id;
            public string parameter;
            public int type;

            public float floatVal;
            public int intVal;
            public bool boolVal;
            public string triggerVal;
        }

        public enum WebAnimValueType
        {
            TYPEFLOAT,
            TYPEINT,
            TYPEBOOL,
            TYPETRIGGER
        }

        #endregion STRUCT

        #region MESSAGE

        [System.Serializable]
        public struct MSG_SyncAnim : IPacketable
        {
            public static int pktId;

            static MSG_SyncAnim() => pktId = nameof(MSG_SyncAnim).GetHashCode();

            public Address64 networkedId;
            public WebAnimState animState;

            public byte[] Marshall() => IPacketable.MarshallJson(pktId, this);

            public static void UnMarshall(byte[] bytes, out MSG_SyncAnim @object) => IPacketable.UnMarshallJson(bytes, out @object);
        }

        #endregion MESSAGE

        [SerializeField] private Animator m_animator;

        private Hashtable m_parameters = new Hashtable();

        public static bool mchCallbackRegisted = false;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public virtual void SyncAnim(AnimParameter parameter)
        {
            var animState = new WebAnimState
            {
                id = m_networkedId.id,
                parameter = parameter.name
            };

            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Int:
                    animState.type = (int)WebAnimValueType.TYPEINT;
                    animState.intVal = m_animator.GetInteger(parameter.name);
                    break;
                case AnimatorControllerParameterType.Float:
                    animState.type = (int)WebAnimValueType.TYPEFLOAT;
                    animState.floatVal = m_animator.GetFloat(parameter.name);
                    break;
                case AnimatorControllerParameterType.Bool:
                    animState.type = (int)WebAnimValueType.TYPEBOOL;
                    animState.boolVal = m_animator.GetBool(parameter.name);
                    break;
                default: // AnimatorControllerParameterType.Trigger:
                    animState.type = (int)WebAnimValueType.TYPETRIGGER;
                    animState.triggerVal = parameter.name;
                    break;
            }

            var @object = new MSG_SyncAnim
            {
                networkedId = m_networkedId.id,
                animState = animState,
            };

            SyncClient.instance.SendWS(@object.Marshall());

            m_syncFromOutside = false;
        }

        public virtual void SyncAnimFromOutside(WebAnimState animState)
        {
            switch (animState.type)
            {
                case (int)WebAnimValueType.TYPEFLOAT:
                    SetFloat(animState.parameter, animState.floatVal);
                    OnChangeParameter(animState.parameter, animState.floatVal.GetHashCode());
                    break;
                case (int)WebAnimValueType.TYPEINT:
                    SetInteger(animState.parameter, animState.intVal);
                    OnChangeParameter(animState.parameter, animState.intVal.GetHashCode());
                    break;
                case (int)WebAnimValueType.TYPEBOOL:
                    SetBool(animState.parameter, animState.boolVal);
                    OnChangeParameter(animState.parameter, animState.boolVal.GetHashCode());
                    break;
                default: // (int)WebAnimValueType.TYPETRIGGER:
                    SetTrigger(animState.parameter);
                    // always false.GetHashCode()
                    break;
            }

            m_syncFromOutside = true;
        }

        protected virtual void OnChangeParameter(string paramName, int hashCode)
        {
            var parameterInfo = m_parameters[paramName] as AnimParameter;

            if (parameterInfo == null)
            {
                Debug.LogError("Animation Parameter Not Found:" + paramName);
                return;
            }

            parameterInfo.lastValueHash = hashCode;
        }

        public virtual void SetInteger(string paramName, int value) => m_animator.SetInteger(paramName, value);

        public virtual void SetFloat(string paramName, float value) => m_animator.SetFloat(paramName, value);

        public virtual void SetBool(string paramName, bool value) => m_animator.SetBool(paramName, value);

        public virtual void SetTrigger(string paramName) => m_animator.SetTrigger(paramName);

        public override void Shutdown()
        {
            if (m_state == State.SHUTDOWNED)
            {
                return;
            }

            Registry<SyncAnimator>.UnRegister(m_networkedId.id);

            base.Shutdown();
        }

        protected virtual void InitParameter()
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
                    default:    //  AnimatorControllerParameterType.Trigger
                        parameterInfo.lastValueHash = false.GetHashCode();
                        break;
                }

                Debug.Log(THIS_NAME + parameter.name);

                m_parameters[parameterInfo.name] = parameterInfo;
            }
        }

        public override void Init(Address32 id)
        {
            base.Init(id);

            InitParameter();

            Registry<SyncAnimator>.Register(m_networkedId.id, this);
        }

        public override void Init()
        {
            base.Init();

            InitParameter();

            Registry<SyncAnimator>.Register(m_networkedId.id, this);
        }

        protected override void Awake()
        {
            if (!mchCallbackRegisted)
            {
                SyncClient.RegisterOnMessage(MSG_SyncAnim.pktId, (from, to, bytes) =>
                {
                    MSG_SyncAnim.UnMarshall(bytes, out var @object);
                    Registry<SyncAnimator>.GetById(@object.networkedId)?.SyncAnimFromOutside(@object.animState);
                });
                mchCallbackRegisted = true;
            }
        }

        protected override void Start()
        {
            base.Start();

            m_animator = GetComponent<Animator>();
        }

        protected override void Update()
        {
            base.Update();

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
                    default:    //  AnimatorControllerParameterType.Trigger
                        currentValueHash = m_animator.GetBool(parameter.name).GetHashCode();
                        parameter.lastValueHash = false.GetHashCode();
                        break;
                }

                if (prevValueHash != currentValueHash)
                {
                    SyncAnim(parameter);
                }
            }
        }

        protected override void OnDestroy()
        {
            Shutdown();
        }

        protected override void OnApplicationQuit()
        {
            Shutdown();
        }
    }
}
