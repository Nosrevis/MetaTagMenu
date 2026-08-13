using MelonLoader;
using UnityEngine;
using UnityEngine.XR;

[assembly: MelonInfo(typeof(MetaTagMenu.MenuMod), "MetaTagMenu", "1.0.0", "Nosrevis")]
[assembly: MelonGame(null, null)]

namespace MetaTagMenu
{
    public class MenuMod : MelonMod
    {
        private bool _last;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("=== MetaTagMenu loaded ===");
        }

        public override void OnUpdate()
        {
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (!left.isValid) return;

            left.TryGetFeatureValue(CommonUsages.secondaryButton, out bool pressed);
            if (pressed && !_last)
                MelonLogger.Msg("Y pressed - menu would open here");
            _last = pressed;
        }
    }
}
