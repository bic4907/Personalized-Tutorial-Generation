using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Unity.MLAgentsExamples
{
    public class QuizAnswerParser : MonoBehaviour
    {
        // Start is called before the first frame update
        
        BoardPresetManager boardPresetManager;
        public Match3Board Board;

        void Start()
        {
            boardPresetManager = GetComponent<BoardPresetManager>();
            Board = GetComponent<Match3Board>();

            // Wait for 1s
            

            Debug.Log("QuizAnswerParser: Start");


            StartCoroutine(ProcessQuiz());        
        }
        

        private IEnumerator ProcessQuiz()
        {
            yield return new WaitForSeconds(1.0f);

            Debug.Log("QuizAnswerParser: Start");

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
            };

            foreach (string filename in filenames)
            {
                yield return StartCoroutine(CheckBoardCoroutine(filename));

            }

            // This line will be executed after all coroutines have finished
            Debug.Log("All coroutines have finished");
        }


        private IEnumerator CheckBoardCoroutine(string filename)
        {
            boardPresetManager.LoadBoard(filename);
            yield return new WaitForSeconds(0.1f);

            Debug.Log($"QuizAnswerParser: LoadBoard ({filename})");

            LevelData levelData = new LevelData();
            levelData.levelName = filename;


            var validMoves = Board.ValidMoves();
            foreach (var move in validMoves)
            {

                Debug.Log("QuizAnswerParser: CheckBoard: " + move.MoveIndex);

                Match3Board _board = Board.DeepCopy();
                _board.MakeMove(move);
                _board.MarkMatchedCells();
                _board.SpawnSpecialCells();
        
                var createdPieces = SpecialMatch.GetMatchCount(_board.GetLastCreatedPiece());
                foreach (var (type, count) in createdPieces)
                {
                    if (count > 0)
                    {
                        Debug.Log($"QuizAnswerParser: CheckBoard: {type} {count}");
                        MoveData moveData = new MoveData();
                        moveData.moveIndex = move.MoveIndex;
                        moveData.type = type.ToString();
                        levelData.moveList.Add(moveData);
                    }
                    // m_SkillKnowledge.IncreaseMatchCount(type, count);
                    // m_ManualSkillKnowledge.IncreaseMatchCount(type, count);
                }


            }

            // Convert the data to JSON format
            string json = JsonUtility.ToJson(levelData, true);

            // Specify the path to the file
            string filePath = Path.Combine(Application.dataPath, $"{filename}.json");

            // Write the JSON data to the file asynchronously
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                yield return writer.WriteAsync(json);
            }

            Debug.Log("CheckBoard: JSON data written to " + filePath);
        }
    }

        [System.Serializable]
        public class MoveData
        {
            public int moveIndex;
            public string type;
        }

        [System.Serializable]
        public class LevelData
        {
            public string levelName;
            public List<MoveData> moveList = new List<MoveData>();
        }


}
