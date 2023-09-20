using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Visometry.VisionLib.SDK.Core.Details;

namespace Visometry.VisionLib.SDK.Core
{
    [CustomEditor(typeof(TrackingConfiguration))]
    public class TrackingConfigurationEditor : Editor
    {
        private SerializedProperty legacyPathProperty;
        private SerializedProperty configurationFileReferenceProperty;
        private SerializedProperty licenseFileReferenceProperty;
        private SerializedProperty calibrationFileReferenceProperty;
        private SerializedProperty autoStartTrackingProperty;
        private SerializedProperty extendTrackingWithSLAMProperty;
        private SerializedProperty useInputSelectionProperty;
        private SerializedProperty showOnMobileProperty;

        private SerializedProperty configurationPathProperty;
        private SerializedProperty licensePathProperty;

        private string extendTrackingInfoMessage = "";
        private string inputSelectionInfoMessage = "";

        private bool extendTrackingPropertyDisabled = false;
        private bool inputSelectionPropertyDisabled = false;

        private void OnEnable()
        {
            this.legacyPathProperty = serializedObject.FindProperty("path");
            this.configurationFileReferenceProperty =
                serializedObject.FindProperty("configurationFileReference");
            this.licenseFileReferenceProperty =
                serializedObject.FindProperty("licenseFileReference");
            this.calibrationFileReferenceProperty =
                serializedObject.FindProperty("calibrationFileReference");
            this.autoStartTrackingProperty = serializedObject.FindProperty("autoStartTracking");
            this.extendTrackingWithSLAMProperty =
                serializedObject.FindProperty("extendTrackingWithSLAM");
            this.useInputSelectionProperty = serializedObject.FindProperty("useInputSelection");
            this.showOnMobileProperty = serializedObject.FindProperty("showOnMobileDevices");

            this.configurationPathProperty =
                this.configurationFileReferenceProperty.FindPropertyRelative("uri");
            this.licensePathProperty =
                this.licenseFileReferenceProperty.FindPropertyRelative("uri");

            serializedObject.Update();
            if (this.legacyPathProperty.stringValue != "")
            {
                SetConfigurationUriFromLegacyPathProperty();
            }

            OverwriteParametersWithConfigurationQuery();
            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.PropertyField(this.configurationFileReferenceProperty);
            }
            if (EditorGUI.EndChangeCheck())
            {
                OverwriteParametersWithConfigurationQuery();
            }

            EditorGUILayout.PropertyField(this.licenseFileReferenceProperty);

            if (this.licensePathProperty.stringValue == "")
            {
                DisplayInfoHelpbox(
                    "No license file set. Using the license, which is specified in the TrackingManager.");
            }

            EditorGUILayout.PropertyField(this.calibrationFileReferenceProperty);

            EditorGUILayout.PropertyField(this.autoStartTrackingProperty);

            using(new EditorGUI.DisabledScope(this.extendTrackingPropertyDisabled))
            {
                EditorGUILayout.PropertyField(this.extendTrackingWithSLAMProperty);
            }

            DisplayInfoHelpbox(this.extendTrackingInfoMessage);

            using(new EditorGUI.DisabledScope(this.inputSelectionPropertyDisabled))
            {
                EditorGUILayout.PropertyField(this.useInputSelectionProperty);

                if (this.useInputSelectionProperty.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(this.showOnMobileProperty);
                    EditorGUI.indentLevel--;
                }
            }

            DisplayInfoHelpbox(this.inputSelectionInfoMessage);

            serializedObject.ApplyModifiedProperties();
        }

        private void DisplayInfoHelpbox(string infoMessage)
        {
            if (infoMessage != "")
            {
                EditorGUILayout.HelpBox(infoMessage, MessageType.Info);
            }
        }

        private void OverwriteParametersWithConfigurationQuery()
        {
            this.extendTrackingInfoMessage = "";
            this.inputSelectionInfoMessage = "";

            this.inputSelectionPropertyDisabled = false;
            this.extendTrackingPropertyDisabled = false;

            Dictionary<string, string> queryMap =
                PathHelper.GetQueryMap(this.configurationPathProperty.stringValue);

            if (queryMap.ContainsKey("tracker.parameters.extendibleTracking"))
            {
                this.extendTrackingInfoMessage =
                    "Overwriting the Extend Tracking parameter with a custom value from the query string.";
                this.extendTrackingPropertyDisabled = true;
            }

            if (queryMap.Keys.Any(
                    key => key == "input.useDeviceID" || key == "input.useImageSource" ||
                           key.StartsWith("inputs") || key.StartsWith("input.imageSources")))
            {
                this.inputSelectionInfoMessage =
                    "Disabling the input selection: A custom input is set via query string.";
                this.inputSelectionPropertyDisabled = true;
            }
        }

        private void SetConfigurationUriFromLegacyPathProperty()
        {
            this.configurationPathProperty.stringValue = PathHelper.CombinePaths(
                "streaming-assets-dir:VisionLib", this.legacyPathProperty.stringValue);

            this.legacyPathProperty.stringValue = "";
        }
    }
}
