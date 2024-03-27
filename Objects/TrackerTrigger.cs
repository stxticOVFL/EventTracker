using UnityEngine;
using PlacementShapes = EventTracker.EventTracker.PlacementShapes;
using MelonLoader.TinyJSON;
using System;
using System.Globalization;
using System.Linq;

namespace EventTracker.Objects
{
    public class TrackerTrigger : MonoBehaviour
    {
        public static Transform holder = null;
        public PlacementShapes shape = PlacementShapes.Plane;

        public Color color = Color.clear;
        public int hitCount = 1;
        public bool grounded = false;
        float opacity { get { return opacityT.result; } }
        bool hasHit = false;
        int multiHit = 0;

        public Color popColor
        {
            get
            {
                Color.RGBToHSV(color, out float H, out _, out _);
                return Color.HSVToRGB(H, .8f, .8f);
            }
        }

        public bool placed = false;

        MeshRenderer renderer;
        Collider collider;
        public TrackerItem.Transition opacityT = new(AxKEasing.EaseOutQuad, null);

        public static TrackerTrigger Spawn(PlacementShapes shape)
        {
            PrimitiveType type = PrimitiveType.Quad;
            switch (shape)
            {
                case PlacementShapes.Cube: type = PrimitiveType.Cube; break;
                case PlacementShapes.Sphere: type = PrimitiveType.Sphere; break;
            }
            var ret = GameObject.CreatePrimitive(type).AddComponent<TrackerTrigger>();
            ret.placed = true;
            ret.enabled = true;
            ret.transform.parent = holder;
            ret.name = $"Trigger {holder.childCount}";
            ret.shape = shape;
            if (type == PrimitiveType.Quad)
            {
                ret.GetComponent<MeshCollider>().convex = true;
                ret.GetComponent<MeshCollider>().isTrigger = true;
            }
            return ret;
        }

        public void Awake()
        {
            opacityT.speed = 5;
            opacityT.result = 1;

            renderer = GetComponent<MeshRenderer>();
            renderer.material.shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended"); // tried many different shaders but alas...
            renderer.sortingOrder = 1;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            Update();

            collider = GetComponent<Collider>();
            collider.isTrigger = true;
        }

        private void Update()
        {
            color.a = EventTracker.Settings.PlaceVisible.Value ? opacity * .2f : 0;
            if (shape == PlacementShapes.Plane && hitCount != 0)
            {
                Vector3 forward = transform.TransformDirection(Vector3.forward);
                Vector3 toOther = RM.playerPosition - transform.position;

                if (Vector3.Dot(forward, toOther) >= 0)
                    color.a = 0;
            }
            renderer.material.SetColor("_TintColor", color);
            opacityT.Process();
            if (!opacityT.running && opacity == 0)
                gameObject.SetActive(false);
        }

        private void OnTriggerStay(Collider other)
        {
            if (hasHit || placed || other.name != "Player" || hitCount == 0)
                return;

            if (shape == PlacementShapes.Plane)
            {
                Vector3 forward = transform.TransformDirection(Vector3.forward);
                Vector3 toOther = RM.playerPosition - transform.position;

                if (Vector3.Dot(forward, toOther) >= 0)
                    return;
            }

            if (grounded && !RM.drifter.GetIsGrounded())
                return;

            hasHit = true;

            if (--hitCount == 0)
                opacityT.Start(1, 0);
            else if (multiHit == 0)
                multiHit = hitCount + 1;
            EventTracker.holder.PushText(name + (multiHit != 0 ? $" (Hit {multiHit - hitCount})" : ""), popColor, !EventTracker.JSON.GetSetting("triggers"), true);
        }

        private void OnTriggerExit(Collider other)
        {
            hasHit = false;
        }

        public string Encode()
        {
            string pos = transform.position.ToString();
            pos = pos.Substring(1, pos.Length - 2);
            string rot = transform.rotation.eulerAngles.ToString();
            rot = rot.Substring(1, rot.Length - 2);
            string scl = transform.localScale.ToString();
            scl = scl.Substring(1, scl.Length - 2);
            return $$"""
            {
                "name": "{{name}}",
                "shape": "{{shape}}",
                "color": "#{{(byte)(color.r * 255):X2}}{{(byte)(color.g * 255):X2}}{{(byte)(color.b * 255):X2}}",
                "hitCount": {{hitCount}},
                "position": "{{pos}}",
                "rotation": "{{rot}}",
                "size": "{{scl}}",
                "grounded": {{grounded.ToString().ToLower()}}
            }
            """;
        }
        public static TrackerTrigger Decode(Variant data)
        {
            TrackerTrigger ret = null;
            try
            {
                var shape = (PlacementShapes)Enum.Parse(typeof(PlacementShapes), data["shape"]);
                ret = Spawn(shape);
                ret.placed = false;
                ret.name = data["name"];
                string colorHex = data["color"];
                ret.color.r = byte.Parse(colorHex.Substring(1, 2), NumberStyles.HexNumber) / 255.0f;
                ret.color.g = byte.Parse(colorHex.Substring(3, 2), NumberStyles.HexNumber) / 255.0f;
                ret.color.b = byte.Parse(colorHex.Substring(5, 2), NumberStyles.HexNumber) / 255.0f;
                ret.hitCount = data["hitCount"];
                float[] split = ((string)data["position"]).Split(',')
                    .Select(x => x.Trim())
                    .Select(float.Parse).ToArray();
                ret.transform.position = new Vector3(split[0], split[1], split[2]);
                split = ((string)data["rotation"]).Split(',')
                    .Select(x => x.Trim())
                    .Select(float.Parse).ToArray();
                ret.transform.rotation = Quaternion.Euler(split[0], split[1], split[2]);
                split = ((string)data["size"]).Split(',')
                    .Select(x => x.Trim())
                    .Select(float.Parse).ToArray();
                ret.transform.localScale = new Vector3(split[0], split[1], split[2]);
                ret.grounded = data["grounded"];
                return ret;
            }
            catch (Exception e)
            {
                Debug.LogError($"error parsing {ret?.name}" + e);
                if (ret)
                    ret.gameObject.SetActive(false);
                return null;
            }
        }
    }
}