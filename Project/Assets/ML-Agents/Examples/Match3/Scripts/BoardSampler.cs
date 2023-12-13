using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Integrations.Match3;
using UnityEngine;

namespace Unity.MLAgentsExamples
{

    public class BoardSampler
    {

        private static BoardSampler _Instance = null;
        private float KnowledgeAlmostRatio = 1.0f;

        public static BoardSampler Instance { get {
            if (_Instance == null)
            {
                _Instance = new BoardSampler();
            }
            return _Instance;
        }}

        public float FillEmpty(Match3Board board, SkillKnowledge knowledge, int Simulation = 100)
        {
            int _simulation = 0;

            Match3Board _tmpBoard = board.DeepCopy();
            _tmpBoard.FillFromAbove();
            Match3Board bestBoard = _tmpBoard;
            
            float bestBoardScore = -99999f;

            for (_simulation = 0; _simulation < Simulation; _simulation++) {

                Match3Board _completedBoard = board.DeepCopy();
                SkillKnowledge playerKnowledge = knowledge.DeepCopy();

                SampleValidBoard(_completedBoard);

                Match3Board _simulationBoard = _completedBoard.DeepCopy();

                Move move = GreedyMatch3Solver.GetAction(_simulationBoard);
                _simulationBoard.MakeMove(move);
                _simulationBoard.MarkMatchedCells();
                _simulationBoard.ClearMatchedCells();
                _simulationBoard.SpawnSpecialCells();
                var createdPieces = _simulationBoard.GetLastCreatedPiece();

                float score = 0;
                foreach ((int CellType, int SpecialType) piece in createdPieces)
                {
                    bool isReached = playerKnowledge.IsMatchCountAlmostReachedTarget((PieceType)piece.SpecialType, KnowledgeAlmostRatio);
                    if (!isReached) // Have to learn
                    {
                        score += (float)Math.Pow(MCTS.Instance.PieceScoreWeight[(PieceType)piece.SpecialType], 1);
                        playerKnowledge.IncreaseMatchCount((PieceType)piece.SpecialType);
                    }
                }


                if (score > bestBoardScore || bestBoard == null)
                {
                    bestBoardScore = score;
                    bestBoard = _completedBoard;
                }

                _completedBoard = null;
                _simulationBoard = null;
            }

            board.m_Cells = ((int CellType, int SpecialType)[,])bestBoard.m_Cells.Clone();

            return bestBoardScore;
        }

        private void SampleValidBoard(Match3Board board) // Use pointer reference
        {
            int trial = 0;
            // Debug.Log("SampleValidBoard");
            while (true)
            {
                Match3Board _tmpBoard = board.DeepCopy();
                _tmpBoard.m_Random = new System.Random();
                _tmpBoard.FillFromAbove();

                // Check if the spawned position made a special match
                bool madeMatch = _tmpBoard.MarkMatchedCells();


                if (madeMatch)
                {
                    trial += 1;

                    if (trial > 5)
                    {
                        board.m_Cells = ((int CellType, int SpecialType)[,])_tmpBoard.m_Cells.Clone();
                        _tmpBoard = null;
                        break;
                    }

                    _tmpBoard = null;
                    continue;
                }
                else
                {
                    board.m_Cells = ((int CellType, int SpecialType)[,])_tmpBoard.m_Cells.Clone();
                    _tmpBoard = null;
                    break;
                }

            }
        }

    }

}