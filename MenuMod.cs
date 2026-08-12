using System;
using System.Collections.Generic;
using MelonLoader;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

[assembly: MelonInfo(typeof(MetaTagMenu.MenuMod), "MetaTagMenu", "1.0.0", "Nosrevis")]
[assembly: MelonGame(null, null)]

namespace MetaTagMenu
{
    public class MenuMod : MelonMod
    {
        // ADD YOUR FEATURES HERE. One Register() line per menu button.
        public override void OnInitializeMelon()
        {
            Register("Log my position", LogPosition);
            Register("Brighten scene", ToggleBrightness);
        }

        private void LogPosition()
        {
            if (Camera.main != null)
                MelonLogger.Msg("Head position: " + Camera.main.transform.position);
        }

        private bool _bright;
        private Color _originalAmbient;
        private void ToggleBrightness()
        {
            if (!_bright) _originalAmbient = RenderSettings.ambientLight;
            _bright = !_bright;
            RenderSettings.ambientLight = _bright ? Color.white : _originalAmbient;
        }

        // ---- menu plumbing below ----

        private class Entry
        {
            public string Name;
            public Action Action;
            public TextMeshPro Label;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private GameObject _root;
        private bool _open;
        private bool _lastToggle;
        private bool _lastTrigger;
        private int _hovered = -1;

        private const float RowSpacing = 0.045f;
        private const float HoverRadius = 0.05f;

        private void Register(string name, Action action)
        {
            _entries.Add(new Entry { Name = name, Action = action });
        }

        public override void OnUpdate()
        {
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!left.isValid || !right.isValid) return;

            left.TryGetFeatureValue(CommonUsages.secondaryButton, out bool toggle);
            if (toggle && !_lastToggle) SetOpen(!_open);
            _lastToggle = toggle;

            if (!_open || _root == null) return;

            PositionMenu(left);
            UpdateHover(right);

            right.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger);
            if (trigger && !_lastTrigger && _hovered >= 0)
            {
                try { _entries[_hovered].Action?.Invoke(); }
                catch (Exception e) { MelonLogger.Error(e); }
            }
            _lastTrigger = trigger;
        }

        private void SetOpen(bool open)
        {
            _open = open;
            if (open && _root == null) Build();
            if (_root != null) _root.SetActive(open);
        }

        private void Build()
        {
            _root = new GameObject("MetaTagMenu");
            UnityEngine.Object.DontDestroyOnLoad(_root);

            var title = MakeLabel("META TAG MENU", 0f);
            title.color = Color.cyan;

            for (int i = 0; i < _entries.Count; i++)
                _entries[i].Label = MakeLabel(_entries[i].Name, -(i + 1) * RowSpacing);
        }

        private TextMeshPro MakeLabel(string text, float yOffset)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = new Vector3(0f, yOffset, 0f);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 0.4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.rectTransform.sizeDelta = new Vector2(0.3f, RowSpacing);
            return tmp;
        }

        private Transform Rig => Camera.main != null ? Camera.main.transform.parent : null;

        private bool HandWorldPose(InputDevice device, out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero;
            rot = Quaternion.identity;
            if (Rig == null) return false;
            if (!device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 p)) return false;
            if (!device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion r)) return false;
            pos = Rig.TransformPoint(p);
            rot = Rig.rotation * r;
            return true;
        }

        private void PositionMenu(InputDevice left)
        {
            if (!HandWorldPose(left, out Vector3 pos, out Quaternion rot)) return;

            _root.transform.position = pos + rot * new Vector3(0f, 0.1f, 0.05f);
            if (Camera.main != null)
                _root.transform.rotation = Quaternion.LookRotation(
                    _root.transform.position - Camera.main.transform.position);
        }

        private void UpdateHover(InputDevice right)
        {
            _hovered = -1;
            if (!HandWorldPose(right, out Vector3 pos, out _)) return;

            for (int i = 0; i < _entries.Count; i++)
            {
                var label = _entries[i].Label;
                if (label == null) continue;

                bool near = Vector3.Distance(pos, label.transform.position) < HoverRadius;
                if (near) _hovered = i;
                label.color = near ? Color.green : Color.white;
            }
        }
    }
}
