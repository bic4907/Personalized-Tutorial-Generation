using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization; 
 

namespace Unity.MLAgentsExamples
{

    public class BoardPresetManager : MonoBehaviour
    {
        public Match3Board board;
        
        private Dictionary<string, SerializableBoard> loadedBoards = new Dictionary<string, SerializableBoard>();

        void Start()
        {
            if (GetComponent<BoardManualAgent>() != null)
            {
                board = GetComponent<BoardManualAgent>().Board;
            }
            if (GetComponent<BoardPCGAgent>() != null)
            {
                board = GetComponent<BoardPCGAgent>().Board;
            }
            if (board == null)
            {
                board = GetComponent<Match3Board>();
            }
            // Debug.Log("BoardPresetManager: " + board);
            PreloadData();
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public void SaveBoard()
        {
            Debug.Log("BoardPresetManager: SaveBoard");
            // Get filename with date and time

            string filename = "board_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".bin";
            string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, filename);

            board.SaveTo(filePath);
        }

        public bool IsCached(string filename)
        {
            // Check if the board is already loaded with filename
            if (loadedBoards.ContainsKey(filename))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void PreloadData()
        {

            string[] filenames = new string[] {
                "horizon_0",
                "horizon_1",
                "vertical_0",
                "vertical_1",
                "cross_0",
                "cross_1",
                "bomb_0",
                "bomb_1",
                "rocket_0",
                "rocket_1",
                "rainbow_0",
                "rainbow_1",
                "init_0",
                "init_1",
                "init_2",
                "init_3",
                "init_4"
            };

            // Preload data
            AssetLoader assetLoader = GameObject.Find("AssetLoader").GetComponent<AssetLoader>();
            
            
            foreach (string filename in filenames)
            {
                var _filename = filename + ".bin";
                string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, _filename);

                Debug.Log("BoardPresetManager: PreloadData: " + filePath);
                assetLoader.LoadAssetBundle(filePath, OnLoadCompleteWithoutBoard);
            }





        }



        public void LoadBoard(string filename)
        {
            filename = filename + ".bin";
            Debug.Log($"BoardPresetManager: LoadBoard ({filename})");
            // Get filename with date and time
            string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, filename);

            if (board != null)
            {
                GetBoardFromStreamingAssets(filePath, board);
            }
            else
            {
                Debug.Log("BoardPresetManager: board is null");
            }
        }


        private void OnLoadCompleteWithoutBoard(string path, byte[] assetBundleData)
        {       
            if (assetBundleData != null)
            {
                // Create a memory stream from the downloaded data
                MemoryStream stream = new MemoryStream(assetBundleData);
                IFormatter formatter = new BinaryFormatter();
                SerializableBoard loadedBoard = (SerializableBoard)formatter.Deserialize(stream);
                var m_Cells = ((int CellType, int SpecialType)[,])loadedBoard.m_Cells.Clone();
                
                var _path = path.Replace("file://", "");
                
                if (!IsCached(_path)) 
                {
                    loadedBoards.Add(_path, loadedBoard);
                    Debug.Log("BoardPresetManager: Board loaded and cached");
                }
            }
        }


        private void OnLoadComplete(string path, byte[] assetBundleData)
        {       
            if (assetBundleData != null && board != null)
            {
                // Create a memory stream from the downloaded data
                MemoryStream stream = new MemoryStream(assetBundleData);
                IFormatter formatter = new BinaryFormatter();
                SerializableBoard loadedBoard = (SerializableBoard)formatter.Deserialize(stream);
                var m_Cells = ((int CellType, int SpecialType)[,])loadedBoard.m_Cells.Clone();
                
                var _path = path.Replace("file://", "");
                loadedBoards.Add(_path, loadedBoard);
                Debug.Log("BoardPresetManager: Board loaded and cached");

                board.LoadCells(m_Cells);
            }
        }


        public void LoadBoard(Match3Board board, string filename)
        {
            this.board = board;

            filename = filename + ".bin";
            Debug.Log($"BoardPresetManager: LoadBoard ({filename})");
            // Get filename with date and time
            string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, filename);

            GetBoardFromStreamingAssets(filePath, board);

        }

        
        public bool GetBoardFromStreamingAssets(string path, Match3Board board)
        {
            bool IsSuccess = false;
            Debug.Log("LoadFrom: " + path);

            if (IsCached(path))
            {
                Debug.Log("BoardPresetManager: Board is already loaded");
                var loadedBoard = loadedBoards[path];
                board.LoadCells(loadedBoard.m_Cells);
                return true;
            }
            else
            {
                Debug.Log("BoardPresetManager: Board is not loaded, downloading...");

                if (path.Contains("http"))
                {
                    AssetLoader assetLoader = GameObject.Find("AssetLoader").GetComponent<AssetLoader>();
                    assetLoader.LoadAssetBundle(path, OnLoadComplete);
                    IsSuccess = true;
                }
                else
                {
                    IFormatter formatter = new BinaryFormatter();
                    Stream streamFileRead = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    SerializableBoard loadedBoard = (SerializableBoard)formatter.Deserialize(streamFileRead);
                    
                    // Add to cache
                    loadedBoards.Add(path, loadedBoard);
                    Debug.Log("BoardPresetManager: Board loaded and cached");

                    board.m_Cells = ((int CellType, int SpecialType)[,])loadedBoard.m_Cells.Clone();
                    IsSuccess = true;
                }
            }



            // }
            // catch (Exception e)
            // {
            //     Debug.Log(e);
            // }
            return IsSuccess;
        }
        

    }


    [Serializable]
    public class SerializableBoard
    {
        public (int CellType, int SpecialType)[,] m_Cells;

        public SerializableBoard(Match3Board board)
        {
            m_Cells = ((int CellType, int SpecialType)[,])board.m_Cells.Clone();
        }

    }
}
