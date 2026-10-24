using System;
using UnityEngine;

namespace Ascendance
{
    /// <summary>
    /// Standard attachment points for production character prefabs. Existing sockets are
    /// discovered by common naming conventions. Missing sockets are created as safe fallbacks.
    /// </summary>
    public sealed class EquipmentSocketRig : MonoBehaviour
    {
        public Transform RightHand { get; private set; }
        public Transform LeftHand { get; private set; }
        public Transform Back { get; private set; }
        public Transform Hip { get; private set; }
        public Transform Staff { get; private set; }
        public Transform Bow { get; private set; }

        public void Initialize(Transform modelRoot)
        {
            RightHand = FindOrCreate(modelRoot, "Socket_RightHand", "RightHand", "Hand_R", "hand_r", "Right Hand", "mixamorig:RightHand");
            LeftHand = FindOrCreate(modelRoot, "Socket_LeftHand", "LeftHand", "Hand_L", "hand_l", "Left Hand", "mixamorig:LeftHand");
            Back = FindOrCreate(modelRoot, "Socket_Back", "Back", "Spine2", "spine_03", "mixamorig:Spine2");
            Hip = FindOrCreate(modelRoot, "Socket_Hip", "Hip", "Hips", "pelvis", "mixamorig:Hips");
            Staff = FindOrCreate(modelRoot, "Socket_Staff", "StaffSocket", "Staff", "Socket_Staff");
            Bow = FindOrCreate(modelRoot, "Socket_Bow", "BowSocket", "Bow", "Socket_Bow");
        }

        public Transform Get(string slot)
        {
            return slot?.ToLowerInvariant() switch
            {
                "righthand" or "right_hand" or "weapon" => RightHand,
                "lefthand" or "left_hand" or "offhand" => LeftHand,
                "back" => Back,
                "hip" => Hip,
                "staff" => Staff,
                "bow" => Bow,
                _ => RightHand
            };
        }

        public GameObject Attach(GameObject prefab, string slot, bool clearExisting = false)
        {
            if (prefab == null) return null;
            Transform anchor = Get(slot);
            if (anchor == null) return null;

            if (clearExisting)
            {
                for (int i = anchor.childCount - 1; i >= 0; i--) Destroy(anchor.GetChild(i).gameObject);
            }

            var instance = Instantiate(prefab, anchor);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static Transform FindOrCreate(Transform root, string fallbackName, params string[] aliases)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (string alias in aliases)
                {
                    if (string.Equals(t.name, alias, StringComparison.OrdinalIgnoreCase)) return t;
                }
            }

            var socket = new GameObject(fallbackName).transform;
            socket.SetParent(root, false);
            return socket;
        }
    }
}
