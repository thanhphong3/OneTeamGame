using UnityEditor;
using UnityEngine;

namespace Visometry.VisionLib.SDK.Core
{
    /// <summary>
    /// Editor script modifying and displaying relevant WorkSpace values.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AdvancedWorkSpace))]
    public class AdvancedWorkSpaceEditor : WorkSpaceEditor
    {
        private SerializedProperty sourceObjectProperty;
        private SerializedProperty rootTransformProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            InitializeSerializedProperties();
        }

        private void InitializeSerializedProperties()
        {
            this.sourceObjectProperty = serializedObject.FindProperty("sourceObject");
            this.rootTransformProperty = serializedObject.FindProperty("rootTransform");
        }

        protected override void DisplaySource()
        {
            TrackingModel trackingModel = null;
            if (base.workSpace.destinationObject)
            {
                trackingModel = base.workSpace.destinationObject.GetComponent<TrackingModel>();
            }
            using (new EditorGUI.DisabledScope(trackingModel != null))
            {
                EditorGUILayout.PropertyField(this.rootTransformProperty);
            }
            using (
                new EditorGUI.DisabledScope(this.sourceObjectProperty.objectReferenceValue != null))
            {
                EditorGUILayout.PropertyField(this.sourceObjectProperty);
            }

            this.serializedObject.ApplyModifiedProperties();
        }

        protected override void UpdateMeshes()
        {
            var useCameraRotation = false;
            base.poses =
                base.workSpace.GetWorkSpaceDefinition(useCameraRotation).GetCameraTransforms();
            base.workSpace.GetSourceGeometry().UpdateMesh();
        }

        [DrawGizmo(GizmoType.Pickable | GizmoType.Selected)]
        private static void DrawGizmos(AdvancedWorkSpace workSpace, GizmoType gizmoType)
        {
            if (workSpace.sourceObject == null)
            {
                return;
            }

            WorkSpaceEditor.DrawGizmos(workSpace, workSpace.sourceObject.transform);
        }

        protected override void SetLocalCameraTransform(Vector3 position, Quaternion orientation, Camera usedCamera)
        {
            usedCamera.transform.position = position;
            usedCamera.transform.rotation = orientation;
        }
    }
}
