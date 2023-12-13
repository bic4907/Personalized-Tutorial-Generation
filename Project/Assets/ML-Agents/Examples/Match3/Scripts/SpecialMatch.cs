using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgentsExamples;


namespace Unity.MLAgentsExamples
{

    public class SpecialMatch
    {

        public static SpecialMatch Instance;
        
        public Dictionary<PieceType, List<int[,]>> MatchCases;
        public Dictionary<PieceType, int> CreateScores;
        public Dictionary<PieceType, int> DestroyScores;

        public static SpecialMatch GetInstance()
        {
            if (Instance == null)
            {
                Instance = new SpecialMatch();
            }
            return Instance;
        }


        public SpecialMatch()
        {
            InitializeMatchCase();
            InitializeCreateScore();
            InitializeDestroyScore();
        }
        
        private void InitializeMatchCase()
        {
            MatchCases = new Dictionary<PieceType, List<int[,]>>();

            MatchCases.Add(PieceType.RainbowPiece, new List<int[,]>());
            MatchCases.Add(PieceType.BombPiece, new List<int[,]>());
            MatchCases.Add(PieceType.CrossPiece, new List<int[,]>());
            MatchCases.Add(PieceType.RocketPiece, new List<int[,]>());
            MatchCases.Add(PieceType.VerticalPiece, new List<int[,]>());
            MatchCases.Add(PieceType.HorizontalPiece, new List<int[,]>());
            MatchCases.Add(PieceType.NormalPiece, new List<int[,]>());



            MatchCases[PieceType.NormalPiece].Add(new int[1, 3] { { 1, 1, 1 } });
            MatchCases[PieceType.NormalPiece].Add(new int[3, 1] { { 1 }, { 1 }, { 1 } });

            MatchCases[PieceType.HorizontalPiece].Add(new int[1, 4] { { 1, 1, 1, 1 } });
            MatchCases[PieceType.VerticalPiece].Add(new int[4, 1] { { 1 }, { 1 }, { 1 }, { 1 } });

            MatchCases[PieceType.RocketPiece].Add(new int[2, 2] { { 1, 1 }, { 1, 1 } });

            MatchCases[PieceType.CrossPiece].Add(new int[3, 3] { { 1, 1, 1 }, 
                                                                 { 0, 1, 0 },
                                                                 { 0, 1, 0 } });
            MatchCases[PieceType.CrossPiece].Add(new int[3, 3] { { 1, 0, 0 },
                                                                 { 1, 1, 1 },
                                                                 { 1, 0, 0 } });
            MatchCases[PieceType.CrossPiece].Add(new int[3, 3] { { 0, 1, 0 },
                                                                 { 0, 1, 0 },
                                                                 { 1, 1, 1 } });
            MatchCases[PieceType.CrossPiece].Add(new int[3, 3] { { 0, 0, 1 },
                                                                 { 1, 1, 1 },
                                                                 { 0, 0, 1 } });

            MatchCases[PieceType.BombPiece].Add(new int[3, 3] { { 1, 1, 1 },
                                                                { 1, 0, 0 },
                                                                { 1, 0, 0 } });
            MatchCases[PieceType.BombPiece].Add(new int[3, 3] { { 1, 1, 1 },
                                                                { 0, 0, 1 },
                                                                { 0, 0, 1 } });
            MatchCases[PieceType.BombPiece].Add(new int[3, 3] { { 0, 0, 1 },
                                                                { 0, 0, 1 },
                                                                { 1, 1, 1 } });
            MatchCases[PieceType.BombPiece].Add(new int[3, 3] { { 1, 0, 0 },
                                                                { 1, 0, 0 },
                                                                { 1, 1, 1 } });


            MatchCases[PieceType.RainbowPiece].Add(new int[1, 5] { { 1, 1, 1, 1, 1 } });
            MatchCases[PieceType.RainbowPiece].Add(new int[5, 1] { { 1 }, { 1 }, { 1 }, { 1 }, { 1 } });
        }

        private void InitializeCreateScore()
        {
            CreateScores = new Dictionary<PieceType, int>();

            CreateScores.Add(PieceType.Empty, 0);
            CreateScores.Add(PieceType.NormalPiece, 0);
            CreateScores.Add(PieceType.HorizontalPiece, 200);
            CreateScores.Add(PieceType.VerticalPiece, 200);
            CreateScores.Add(PieceType.CrossPiece, 500);
            CreateScores.Add(PieceType.BombPiece, 500);
            CreateScores.Add(PieceType.RocketPiece, 500);
            CreateScores.Add(PieceType.RainbowPiece, 1000);
        }

        private void InitializeDestroyScore()
        {
            DestroyScores = new Dictionary<PieceType, int>();
        
            DestroyScores.Add(PieceType.Empty, 0);
            DestroyScores.Add(PieceType.NormalPiece, 1);
            DestroyScores.Add(PieceType.HorizontalPiece, 1);
            DestroyScores.Add(PieceType.VerticalPiece, 1);
            DestroyScores.Add(PieceType.CrossPiece, 1);
            DestroyScores.Add(PieceType.BombPiece, 1);
            DestroyScores.Add(PieceType.RocketPiece, 1);
            DestroyScores.Add(PieceType.RainbowPiece, 2);
        }

        public int GetCreateScore(PieceType pieceType)
        {
            return CreateScores[pieceType];
        }

        public int GetDestroyScore(PieceType pieceType)
        {
            return DestroyScores[pieceType];
        }

        public static Dictionary<PieceType, int> GetMatchCount(List<(int CellType, int SpecialType)> pieceList, bool includeNormalPiece = false)
        {
            List<PieceType> targetPieces = new List<PieceType> {PieceType.HorizontalPiece, PieceType.VerticalPiece, PieceType.CrossPiece, PieceType.BombPiece, PieceType.RocketPiece, PieceType.RainbowPiece};
            if (includeNormalPiece) targetPieces.Add(PieceType.NormalPiece);
            
            Dictionary<PieceType, int> matchCount = new Dictionary<PieceType, int>();
            
            foreach (var piece in targetPieces) // Initialize
            {
                matchCount.Add(piece, 0);
            }
            
            foreach (var piece in pieceList)
            {
                matchCount[(PieceType)piece.SpecialType] += 1;
            }

            return matchCount;
        }

    }
}