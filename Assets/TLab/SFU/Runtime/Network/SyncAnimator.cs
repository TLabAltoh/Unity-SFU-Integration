using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace TLab.SFU.Network
{
    [AddComponentMenu("TLab/SFU/Sync Animator (TLab)")]
    public class SyncAnimator : NetworkedObject
    {
        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        public static void Register(string id, SyncAnimator syncAnimator) => m_registry[id] = syncAnimator;

        public static new void UnRegister(string id) => m_registry.Remove(id);

        public static new void ClearRegistry()
        {
            var gameobjects = new List<GameObject>();

            foreach (DictionaryEntry entry in m_registry)
            {
                var grabbable = entry.Value as SyncAnimator;
                gameobjects.Add(grabbable.gameObject);
            }

            for (int i = 0; i < gameobjects.Count; i++)
            {
                Destroy(gameobjects[i]);
            }

            m_registry.Clear();
        }

        public static new SyncAnimator GetById(string id) => m_registry[id] as SyncAnimator;

        #endregion REGISTRY

        #region STRUCT

        [System.Serializable]
        public class AnimParameter
        {
            public AnimatorControllerParameterType type;
            public string name;
            public int lastValueHash;
        }

        [System.Serializable]
        public class WebAnimState
        {
            public string id;
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

        #region MESSAGE_TYPE

        [System.Serializable]
        public class MCH_SyncAnim
        {
            public string networkedId;
            public WebAnimState animState;
        }

        #endregion MESSAGE_TYPE

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

            var message = JsonUtility.ToJson(animState);

            var obj = new MasterChannelJson
            {
                messageType = nameof(WebAnimState),
                message = message,
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

            SyncClient.instance.MasterChannelSend(obj);

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

            UnRegister(m_networkedId.id);

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

        public override void Init(string id)
        {
            base.Init(id);

            InitParameter();

            Register(m_networkedId.id, this);
        }

        public override void Init()
        {
            base.Init();

            InitParameter();

            Register(m_networkedId.id, this);
        }

        protected override void Awake()
        {
            if (!mchCallbackRegisted)
            {
                SyncClient.RegisterMasterChannelCallback(nameof(MCH_SyncAnim), (obj) =>
                {
                    var json = JsonUtility.FromJson<MCH_SyncAnim>(obj.message);

                    GetById(json.networkedId)?.SyncAnimFromOutside(json.animState);
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
