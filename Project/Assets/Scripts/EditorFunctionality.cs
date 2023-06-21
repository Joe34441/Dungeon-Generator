using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MazeGenerator))]
public class EditorFunctionality : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
		
        GUILayout.Label(" ");
		
        MazeGenerator script = (MazeGenerator)target;
        if (GUILayout.Button("Generate"))
        {
            script.Generate();
        }
        if (GUILayout.Button("Save Current"))
        {
            script.SaveCurrent();
        }
        if (GUILayout.Button("Load Saved"))
        {
            script.GenerateSaved();
        }
        if (GUILayout.Button("Refresh Markers"))
        {
            script.RefreshMarkers();
        }
        if (GUILayout.Button("Destroy"))
        {
            script.DestroyAll();
        }
    }
}
