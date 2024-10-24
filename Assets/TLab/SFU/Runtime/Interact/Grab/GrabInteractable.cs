using UnityEngine;

namespace TLab.SFU.Interact
{
    using Registry = Registry<GrabInteractable>;

    [AddComponentMenu("TLab/SFU/Grab Interactable (TLab)")]
    public class GrabInteractable : Interactable
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            Registry.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Registry.UnRegister(this);
        }
    }
}
