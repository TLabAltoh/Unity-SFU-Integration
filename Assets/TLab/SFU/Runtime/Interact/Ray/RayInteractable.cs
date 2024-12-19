using UnityEngine;

namespace TLab.SFU.Interact
{
    using Registry = Registry<RayInteractable>;

    [AddComponentMenu("TLab/SFU/Ray Interactable (TLab)")]
    public class RayInteractable : Interactable
    {
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
    }
}
