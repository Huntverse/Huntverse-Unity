using UnityEditor;
using UnityEngine;

namespace Hunt
{
    [CustomEditor(typeof(SwordTrailEffect))]
    public class SwordTrailEffectEditor : Editor
    {
        private void OnSceneGUI()
        {
            var effect = (SwordTrailEffect)target;
            SerializedObject so = new SerializedObject(effect);
            SerializedProperty pStart = so.FindProperty("bladeStartLocal");
            SerializedProperty pEnd = so.FindProperty("bladeEndLocal");
            if (pStart == null || pEnd == null) return;

            Transform t = effect.transform;
            Vector3 startWorld = t.TransformPoint(pStart.vector3Value);
            Vector3 endWorld = t.TransformPoint(pEnd.vector3Value);

            EditorGUI.BeginChangeCheck();
            Handles.color = Color.blue;
            startWorld = Handles.PositionHandle(startWorld, Quaternion.identity);
            Handles.color = Color.red;
            endWorld = Handles.PositionHandle(endWorld, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(effect, "Blade points");
                so.Update();
                pStart.vector3Value = t.InverseTransformPoint(startWorld);
                pEnd.vector3Value = t.InverseTransformPoint(endWorld);
                so.ApplyModifiedProperties();
            }
        }
    }
}
