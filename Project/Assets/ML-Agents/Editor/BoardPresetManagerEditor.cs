using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Unity.MLAgentsExamples
{
    [CustomEditor(typeof (BoardPresetManager))]
    public class BoardPresetManagerEditor: Editor
    {
        private int selectedFileIndex = 0;
        private List<string> fileNames = new List<string>();

        private int m_CntOnInspectorGUI = 0;

        public override void OnInspectorGUI()
        {

            base.OnInspectorGUI();  

            m_CntOnInspectorGUI++;

            string folderPath = Application.streamingAssetsPath;
            string[] filePaths = Directory.GetFiles(folderPath, "*.bin");

            fileNames.Clear();
            foreach (string filePath in filePaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                fileNames.Add(fileName);
            }

            selectedFileIndex = EditorGUILayout.Popup("Select a file", selectedFileIndex, fileNames.ToArray());
        



            BoardPresetManager bpm = target as BoardPresetManager;
            if (GUILayout.Button("Save Board")) {

                if (bpm)
                {
                    bpm.SaveBoard();
                }

            }
            if (GUILayout.Button("Load Board")) {

                if (bpm)
                {
                    bpm.LoadBoard(fileNames[selectedFileIndex]);
                }

            }
        }

    }
}