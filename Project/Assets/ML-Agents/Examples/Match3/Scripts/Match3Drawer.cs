using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;
using Unity.MLAgents.Integrations.Match3;

namespace Unity.MLAgentsExamples
{
    public class Match3Drawer : MonoBehaviour
    {
        public int DebugMoveIndex = -1;

        AgentType currentAgentType;

        static Color[] s_Colors = new[]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.cyan,
            Color.magenta,
            Color.yellow,
            Color.gray,
            Color.black,
        };
        private static Color s_EmptyColor = new Color(0.5f, 0.5f, 0.5f, .25f);

        public Dictionary<(int, int), Match3TileSelector> tilesDict = new Dictionary<(int, int), Match3TileSelector>();
        public float CubeSpacing = 1.25f;
        public GameObject TilePrefab;
        private bool m_Initialized;
        private Match3Board m_Board;
        bool OnlyExplodeOnce = true;
        public GameObject explosionPrefab;
        public GameObject glowPrefab;
        int matchedCellCount = 0;

        void Awake()
        {
            if (!m_Initialized)
            {
                InitializeDict();
            }
            
            // Get agent type but, if it's not a board agent, set it to BoardManualAgent
            if (GetComponent<BoardPCGAgent>() != null)
            {
                currentAgentType = GetComponent<BoardPCGAgent>().agentType;
            }
            if  (GetComponent<BoardManualAgent>() != null)
            {
                currentAgentType = GetComponent<BoardManualAgent>().agentType;
            }
        }

        void InitializeDict()
        {
            m_Board = GetComponent<Match3Board>();
            foreach (var item in tilesDict)
            {
                if (item.Value)
                {
                    DestroyImmediate(item.Value.gameObject);
                }
            }

            tilesDict.Clear();

            for (var i = 0; i < m_Board.MaxRows; i++)
            {
                for (var j = 0; j < m_Board.MaxColumns; j++)
                {
                    var go = Instantiate(TilePrefab, transform.position, Quaternion.identity, transform);
                    go.name = $"r{i}_c{j}";
                    go.AddComponent(typeof(BoxCollider));

                    tilesDict.Add((i, j), go.GetComponent<Match3TileSelector>());
                    go.GetComponent<Match3TileSelector>().InstantiateTiles(explosionPrefab, glowPrefab);
                }
            }

            m_Initialized = true;
        }

        void Update()
        {
            
            if (!m_Board)
            {
                m_Board = GetComponent<Match3Board>();
            }

            if (!m_Initialized)
            {
                InitializeDict();
            }

            if(currentAgentType == AgentType.Human)
            {
                var matchedCells = m_Board.GetMatchedCells();
                if (matchedCells.Count == 0 || (matchedCells.Count != matchedCellCount))
                {
                    OnlyExplodeOnce = true;
                }

                matchedCellCount = matchedCells.Count;
                
                if(OnlyExplodeOnce)
                {
                    foreach (var item in matchedCells)
                    {
                        tilesDict[(item[1], item[0])].ExplodeTile();
                    }
                    OnlyExplodeOnce = false;
                }
            }

            var currentSize = m_Board.GetCurrentBoardSize();
            for (var i = 0; i < m_Board.MaxRows; i++)
            {
                for (var j = 0; j < m_Board.MaxColumns; j++)
                {
                    int value = Match3Board.k_EmptyCell;
                    int specialType = 0;
                    if (m_Board.Cells != null && i < currentSize.Rows && j < currentSize.Columns)
                    {
                        value = m_Board.GetCellType(i, j);
                        specialType = m_Board.GetSpecialType(i, j) - 1;
                    }
                    var pos = new Vector3(j, i, 0);
                    pos *= CubeSpacing;

                    tilesDict[(i, j)].transform.position = transform.TransformPoint(pos);

                    if(currentAgentType == AgentType.Human)
                    {
                        tilesDict[(i, j)].SetActiveTile(specialType, value, isHumanControlled: true);
                    }
                    else
                    {
                        tilesDict[(i, j)].SetActiveTile(specialType, value, isHumanControlled: false);
                    }
                }
            }
        }

        void OnDrawGizmos()
        {
            Profiler.BeginSample("Match3.OnDrawGizmos");
            var cubeSize = .5f;
            var matchedWireframeSize = .5f * (cubeSize + CubeSpacing);

            if (!m_Board)
            {
                m_Board = GetComponent<Match3Board>();
                if (m_Board == null)
                {
                    return;
                }
            }

            int moveCount = 0;
            int availableCount = 0;

            foreach (var move in m_Board.AllMoves())
            {
                moveCount++;
                

                if (DebugMoveIndex >= 0 && move.MoveIndex != DebugMoveIndex)
                {
                    continue;
                }

                if (!m_Board.IsMoveValid(move))
                {
                    continue;
                }

                var (otherRow, otherCol) = move.OtherCell();
                var pos = new Vector3(move.Column, move.Row, 0) * CubeSpacing;
                var otherPos = new Vector3(otherCol, otherRow, 0) * CubeSpacing;

                var oneQuarter = Vector3.Lerp(pos, otherPos, .25f);
                var threeQuarters = Vector3.Lerp(pos, otherPos, .75f);
                Gizmos.DrawLine(transform.TransformPoint(oneQuarter), transform.TransformPoint(threeQuarters));

                availableCount++;
            }

            // Debug.Log($"Move count: {moveCount}, available: {availableCount}");

            // Pause the unity player
            // EditorApplication.isPaused = true;


            Profiler.EndSample();
        }

        public void GlowTiles(Move move, bool isTwoWay = false)
        {
            tilesDict[(move.Row, move.Column)].GlowTile();
            if (isTwoWay)
            {
                var (otherRow, otherCol) = move.OtherCell();
                tilesDict[(otherRow, otherCol)].GlowTile();
            }
        }

        public void StopGlowingTiles()
        {
            foreach (var item in tilesDict)
            {
                item.Value.StopGlow();
            }
        }
    }
}
