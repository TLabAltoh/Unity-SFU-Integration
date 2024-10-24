using UnityEngine;

namespace TLab.SFU.Interact
{
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

        protected override void OnEnable()
        {
            base.OnEnable();

            Registry<GameObjectGrabbable>.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Registry<GameObjectGrabbable>.UnRegister(this);
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
