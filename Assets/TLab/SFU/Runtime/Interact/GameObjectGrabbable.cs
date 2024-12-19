using UnityEngine;

namespace TLab.SFU.Interact
{
    using Registry = Registry<GameObjectGrabbable>;

    [AddComponentMenu("TLab/SFU/Game Object Grabbable (TLab)")]
    [RequireComponent(typeof(GameObjectController))]
    public class GameObjectGrabbable : Interactable
    {
        private GameObjectController m_controller;

        public GameObjectController controller => m_controller;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        private void IgnoreCollision(Interactor interactor, bool ignore)
        {
            // TODO
        }

        public override void OnSelect(Interactor interactor)
        {
            switch (m_controller.OnGrab(interactor))
            {
                case GameObjectController.HandType.First:
                    IgnoreCollision(interactor, true);
                    break;
                case GameObjectController.HandType.Second:
                    IgnoreCollision(interactor, true);
                    break;
                case GameObjectController.HandType.None:
                    break;
            }

            base.OnSelect(interactor);
        }

        public override void OnUnselect(Interactor interactor)
        {
            switch (m_controller.GetHandType(interactor))
            {
                case GameObjectController.HandType.First:
                    IgnoreCollision(interactor, false);
                    m_controller.OnRelease(interactor);
                    break;
                case GameObjectController.HandType.Second:
                    IgnoreCollision(interactor, false);
                    m_controller.OnRelease(interactor);
                    break;
                case GameObjectController.HandType.None:
                    break;
            }

            base.OnUnselect(interactor);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Registry.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Registry.Unregister(this);
        }

        protected override void Start()
        {
            base.Start();

            m_controller = GetComponent<GameObjectController>();
        }
    }
}
