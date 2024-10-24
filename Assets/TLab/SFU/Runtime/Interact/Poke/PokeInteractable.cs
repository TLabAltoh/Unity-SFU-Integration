using UnityEngine;

namespace TLab.SFU.Interact
{
    using Registry = Registry<PokeInteractable>;

    [AddComponentMenu("TLab/SFU/Poke Interactable (TLab)")]
    public class PokeInteractable : Interactable
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
