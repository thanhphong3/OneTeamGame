using UnityEditor;

namespace Visometry.VisionLib.SDK.Core
{
    [CustomEditor(typeof(RuntimeParameter))]
    public class RuntimeParameterEditor : Editor
    {
        private SerializedProperty parameterNameProp;
        private SerializedProperty parameterTypeProp;
        private SerializedProperty changingProp;
        private SerializedProperty stringValueChangedEventProp;
        private SerializedProperty intValueChangedEventProp;
        private SerializedProperty floatValueChangedEventProp;
        private SerializedProperty boolValueChangedEventProp;

        void OnEnable()
        {
            this.parameterNameProp = serializedObject.FindProperty("parameterName");
            this.parameterTypeProp = serializedObject.FindProperty("parameterType");
            this.changingProp = serializedObject.FindProperty("changing");

            this.stringValueChangedEventProp =
                serializedObject.FindProperty("stringValueChangedEvent");
            this.intValueChangedEventProp = serializedObject.FindProperty("intValueChangedEvent");
            this.floatValueChangedEventProp =
                serializedObject.FindProperty("floatValueChangedEvent");
            this.boolValueChangedEventProp = serializedObject.FindProperty("boolValueChangedEvent");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(this.parameterNameProp);
            EditorGUILayout.PropertyField(this.parameterTypeProp);
            EditorGUILayout.PropertyField(this.changingProp);

            RuntimeParameter.ParameterType parameterType =
                (RuntimeParameter.ParameterType) this.parameterTypeProp.enumValueIndex;
            switch (parameterType)
            {
                case RuntimeParameter.ParameterType.String:
                    EditorGUILayout.PropertyField(this.stringValueChangedEventProp);
                    break;
                case RuntimeParameter.ParameterType.Int:
                    EditorGUILayout.PropertyField(this.intValueChangedEventProp);
                    EditorGUILayout.PropertyField(this.floatValueChangedEventProp);
                    break;
                case RuntimeParameter.ParameterType.Float:
                    EditorGUILayout.PropertyField(this.floatValueChangedEventProp);
                    break;
                case RuntimeParameter.ParameterType.Bool:
                    EditorGUILayout.PropertyField(this.boolValueChangedEventProp);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
