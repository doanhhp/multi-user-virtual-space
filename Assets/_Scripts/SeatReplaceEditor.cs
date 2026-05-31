using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SeatReplacer))]
public class SeatReplacerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);

        SeatReplacer replacer = (SeatReplacer)target;

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.5f);
        if (GUILayout.Button("Replace Seats", GUILayout.Height(40)))
            replacer.ReplaceSeats();

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("Restore Default", GUILayout.Height(30)))
            replacer.RestoreDefaultSeats();

        GUI.backgroundColor = Color.white;
    }
}