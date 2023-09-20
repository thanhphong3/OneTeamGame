using UnityEngine;
using UnityEditor;
using Visometry.VisionLib.SDK.Core.Details;

namespace Visometry.VisionLib.SDK.Core
{
    public class AboutVisionLibWindow : EditorWindow
    {
        private static Texture2D vlLogo;
        private static GUIStyle iconStyle = new GUIStyle();

        [MenuItem("VisionLib/About")]
        static void CreateWindow()
        {
            vlLogo = LoadVLLogo();

            AboutVisionLibWindow window = (AboutVisionLibWindow)
                GetWindow(typeof(AboutVisionLibWindow), true, "About VisionLib");

            window.maxSize = new Vector2(450f, 130f);
            window.minSize = new Vector2(450f, 130f);
            window.Show();
        }

        private static Texture2D LoadVLLogo()
        {
            // Light Mode
            if (!EditorGUIUtility.isProSkin)
            {
                return Resources.Load("VLLogo_170x36_LightTheme", typeof(Texture2D)) as Texture2D;
            }
            // Dark Mode
            else
            {
                return Resources.Load("VLLogo_170x36_DarkTheme", typeof(Texture2D)) as Texture2D;
            }
        }

        void OnGUI()
        {
            iconStyle.margin = new RectOffset(30, 0, 0, 0);

            GUILayout.FlexibleSpace();
            GUILayout.Label(vlLogo, iconStyle);
            GUILayout.FlexibleSpace();

            EditorGUI.indentLevel++;
            EditorGUI.indentLevel++;

            DrawCopyableInformationField(
                "SDK Version",
                API.SystemInfo.GetVLSDKVersion(),
                API.SystemInfo.GetDetailedVLSDKVersion());
            DrawCopyableInformationField("Host ID", API.SystemInfo.GetHostID());

            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;

            GUILayout.FlexibleSpace();
        }

        private void DrawCopyableInformationField(
            string fieldLabel,
            string fieldContent,
            string contentToCopy = null)
        {
            using(var horizontalScope = new GUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(fieldLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField(fieldContent, GUILayout.Width(270));

                if (GUILayout.Button(
                        EditorGUIUtility.IconContent("TreeEditor.Duplicate", "| Copy to clipboard"),
                        GUILayout.Width(30)))
                {
                    if (contentToCopy == null)
                    {
                        contentToCopy = fieldContent;
                    }

                    GUIUtility.systemCopyBuffer = contentToCopy;
                    LogHelper.LogInfo(
                        "Copied " + fieldLabel + " \"" + contentToCopy + "\" to clipboard");
                }
            }
        }
    }
}
