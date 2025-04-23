using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Hands.Gestures;

[CustomEditor(typeof(XRHandShape))]
public class XRHandShapeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();
        
        // Add space
        EditorGUILayout.Space();
        
        // Add a button to open the preview window
        if (GUILayout.Button("Open 3D Preview"))
        {
            // Open the preview window
            XRHandShapeViewerWindow window = EditorWindow.GetWindow<XRHandShapeViewerWindow>("Hand Shape Preview");
            
            // Select the current XRHandShape
            Selection.activeObject = target;
        }
    }
} 