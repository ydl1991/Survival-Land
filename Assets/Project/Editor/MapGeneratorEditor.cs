using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// [CustomEditor(typeof(MapGenerator))]
// public class MapGeneratorEditor : Editor
// {
//     // generate map without click play button
//     public override void OnInspectorGUI()
//     {
//         MapGenerator mapGen = (MapGenerator)target;
//         if (DrawDefaultInspector())
//         {
//             if (mapGen.m_autoGenerate)
//             {
//                 mapGen.DrawMapInEditor();
//             }
//         }

//         if (GUILayout.Button("Generate Map"))
//         {
//             mapGen.DrawMapInEditor();
//         }
//     }
// }
