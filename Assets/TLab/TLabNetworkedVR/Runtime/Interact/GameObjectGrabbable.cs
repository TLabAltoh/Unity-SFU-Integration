using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace TLab.NetworkedVR.Interact
{
    [AddComponentMenu("TLab/NetworkedVR/Game Object Grabbable (TLab)")]
    [RequireComponent(typeof(GameObjectController))]
    public class GameObjectGrabbable : Interactable
    {
        private GameObjectController m_controller;

        public GameObjectController controller => m_controller;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        public static new Hashtable registry => m_registry;

        protected static void Register(string id, GameObjectGrabbable goGrabbable)
        {
            if (!m_registry.ContainsKey(id))
            {
                m_registry[id] = goGrabbable;

                Debug.Log(REGISTRY + "goGrabbable registered in the registry: " + id);
            }
        }

        protected static void UnRegister(string id)
        {
            if (m_registry.ContainsKey(id))
            {
                m_registry.Remove(id);

                Debug.Log(REGISTRY + "deregistered goGrabbable from the registry.: " + id);
            }
        }

        public static void ClearRegistry()
        {
            var gameobjects = new List<GameObject>();

            foreach (DictionaryEntry entry in m_registry)
            {
                var goGrabbable = entry.Value as GameObjectGrabbable;
                gameobjects.Add(goGrabbable.gameObject);
            }

            for (int i = 0; i < gameobjects.Count; i++)
            {
                Destroy(gameobjects[i]);
            }

            m_registry.Clear();
        }

        public static GameObjectGrabbable GetById(string id) => m_registry[id] as GameObjectGrabbable;

        #endregion REGISTRY

        private void IgnoreCollision(Interactor interactor, bool ignore)
        {
            // TODO: add physics base interactor ...

            //if (interactor.physicsHand == null)
            //{
            //    return;
            //}

            //var jointPairs = interactor.physicsHand.jointPairs;
            //foreach (JointPair jointPair in jointPairs)
            //{
            //    jointPair.slave.colliders.ForEach((c) => Physics.IgnoreCollision(c, m_collider, ignore));
            //}
        }

        public override void Selected(Interactor interactor)
        {
            switch (m_controller.OnGrabbed(interactor))
            {
                case GameObjectController.HandType.MAIN_HAND:

                    IgnoreCollision(interactor, true);

                    break;
                case GameObjectController.HandType.SUB_HAND:

                    IgnoreCollision(interactor, true);

                    break;
                case GameObjectController.HandType.NONE:
                    break;
            }

            base.Selected(interactor);
        }

        public override void UnSelected(Interactor interactor)
        {
            switch (m_controller.GetHandType(interactor))
            {
                case GameObjectController.HandType.MAIN_HAND:

                    IgnoreCollision(interactor, false);

                    m_controller.OnRelease(interactor);

                    break;
                case GameObjectController.HandType.SUB_HAND:

                    IgnoreCollision(interactor, false);

                    m_controller.OnRelease(interactor);

                    break;
                case GameObjectController.HandType.NONE:
                    break;
            }

            base.UnSelected(interactor);
        }

        public override void WhileSelected(Interactor interactor)
        {
            base.WhileSelected(interactor);
        }

        protected override void Start()
        {
            base.Start();

            m_controller = GetComponent<GameObjectController>();
        }
    }
}
