using UnityEngine;
using UnityEditor;
using SpaceShooter.Ships;

namespace SpaceShooter.EditorTools
{
    /// <summary>
    /// Authoring tool for ShipHardpoints. Adds buttons to spawn fire/thruster points and draws
    /// movable + rotatable handles in the Scene view. Open a ship (prefab or scene object), add a
    /// ShipHardpoints component, then place muzzles (red, fire along +Z) and thrusters (cyan, emit
    /// along -Z) per hull.
    /// </summary>
    [CustomEditor(typeof(ShipHardpoints))]
    public class ShipHardpointsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var hp = (ShipHardpoints)target;
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Authoring", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ Fire Point")) AddPoint(hp, true);
                if (GUILayout.Button("+ Thruster Point")) AddPoint(hp, false);
            }
            if (GUILayout.Button("Mirror Last Across X (twin guns/engines)")) MirrorLast(hp);

            EditorGUILayout.HelpBox(
                "Fire points (red): projectiles spawn here, firing along the point's blue +Z arrow.\n" +
                "Thruster points (cyan): engine VFX attach here, emitting along -Z.\n" +
                "Select a point and drag/rotate it in the Scene view. 'Mirror' duplicates the last point to the opposite side.",
                MessageType.Info);
        }

        void AddPoint(ShipHardpoints hp, bool fire)
        {
            int idx = fire ? hp.firePoints.Count : hp.thrusterPoints.Count;
            var go = new GameObject((fire ? "FirePoint_" : "ThrusterPoint_") + idx);
            Undo.RegisterCreatedObjectUndo(go, "Add Hardpoint");
            go.transform.SetParent(hp.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, fire ? 1.2f : -1.2f);
            if (fire) hp.firePoints.Add(go.transform); else hp.thrusterPoints.Add(go.transform);
            EditorUtility.SetDirty(hp);
            Selection.activeGameObject = go;
        }

        void MirrorLast(ShipHardpoints hp)
        {
            var list = hp.firePoints.Count > 0 ? hp.firePoints : hp.thrusterPoints;
            bool fire = hp.firePoints.Count > 0;
            if (list.Count == 0 || list[list.Count - 1] == null) return;
            var src = list[list.Count - 1];
            var go = new GameObject(src.name + "_mirror");
            Undo.RegisterCreatedObjectUndo(go, "Mirror Hardpoint");
            go.transform.SetParent(hp.transform, false);
            var lp = src.localPosition; lp.x = -lp.x;
            go.transform.localPosition = lp;
            go.transform.localRotation = src.localRotation;
            list.Add(go.transform);
            EditorUtility.SetDirty(hp);
        }

        void OnSceneGUI()
        {
            var hp = (ShipHardpoints)target;
            DrawHandles(hp.firePoints, new Color(1f, 0.4f, 0.3f), "Fire");
            DrawHandles(hp.thrusterPoints, new Color(0.3f, 0.8f, 1f), "Thrust");
        }

        void DrawHandles(System.Collections.Generic.List<Transform> pts, Color c, string label)
        {
            if (pts == null) return;
            for (int i = 0; i < pts.Count; i++)
            {
                var tr = pts[i];
                if (tr == null) continue;
                Handles.color = c;
                Handles.SphereHandleCap(0, tr.position, Quaternion.identity, 0.18f, EventType.Repaint);
                Handles.Label(tr.position + Vector3.up * 0.25f, label + i);

                EditorGUI.BeginChangeCheck();
                Vector3 np = Handles.PositionHandle(tr.position, tr.rotation);
                Quaternion nr = Handles.RotationHandle(tr.rotation, tr.position);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(tr, "Move Hardpoint");
                    tr.position = np;
                    tr.rotation = nr;
                }
            }
        }
    }
}
