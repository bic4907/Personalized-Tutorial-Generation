using System;
using System.Collections.Generic;
using Unity.MLAgents.Integrations.Match3;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization; 
using UnityEngine.Networking;

namespace Unity.MLAgentsExamples
{

    public class Match3Board : AbstractBoard
    {
        public int MinRows;
        [FormerlySerializedAs("Rows")]
        public int MaxRows;

        public int MinColumns;
        [FormerlySerializedAs("Columns")]
        public int MaxColumns;

        public int NumCellTypes;
        // public int NumSpecialTypes;

        public const int k_EmptyCell = -1;
        //[Tooltip("Points earned for clearing a basic cell (cube)")]
        //public int BasicCellPoints = 1;

        //[Tooltip("Points earned for clearing a special cell (sphere)")]
        //public int SpecialCell1Points = 2;
        
        //[Tooltip("Points earned for clearing an extra special cell (plus)")]
        //public int SpecialCell2Points = 3;



        /// <summary>
        /// Seed to initialize the <see cref="System.Random"/> object.
        /// </summary>
        public int RandomSeed;

        GameObject m_DummyBoard;

        public (int CellType, int SpecialType)[,] m_Cells;

        // Created special blocks in the board
        (int CellType, int SpecialType)[,] m_CreatedCells;


        // PCG에서 새로 생성된 블럭들을 보관하기 위한 공간
        (int CellType, int SpecialType)[,] m_ReadyCells;

        private List<SpecialEffect> m_SpecialEffects;
        private List<(int CellType, int SpecialType)> m_LastCreatedPiece;
        private List<(int CellType, int SpecialType)> m_LastDestroyedPiece;
        private List<(int CellType, int SpecialType)> m_LastSeenCreatedPiece;
        private List<(int CellType, int SpecialType)> m_LastSeenDestroyedPiece;
        private List<(PieceType SpecialType, List<int[]> Positions)> m_SpecialMatchPositions;


        public bool[,] m_Matched;

        private BoardSize m_CurrentBoardSize;

        public System.Random m_Random;

        void Awake()
        {
            m_Cells = new (int, int)[MaxColumns, MaxRows];
            m_CreatedCells = new (int, int)[MaxColumns, MaxRows];
            m_Matched = new bool[MaxColumns, MaxRows];

            // Start using the max rows and columns, but we'll update the current size at the start of each episode.
            m_CurrentBoardSize = new BoardSize
            {
                Rows = MaxRows,
                Columns = MaxColumns,
                NumCellTypes = NumCellTypes,
            };

            m_LastCreatedPiece = new List<(int CellType, int SpecialType)>();
            m_LastDestroyedPiece = new List<(int CellType, int SpecialType)>();

            m_LastSeenCreatedPiece = new List<(int CellType, int SpecialType)>();
            m_LastSeenDestroyedPiece = new List<(int CellType, int SpecialType)>();

            m_SpecialEffects = new List<SpecialEffect>();
            m_SpecialMatchPositions = new List<(PieceType SpecialType, List<int[]> Positions)>();
        
            // Set Dummyboard children game object
            m_DummyBoard = GameObject.Find("DummyBoard").gameObject;
        }

        void Start()
        {
            m_Random = new System.Random(RandomSeed == -1 ? gameObject.GetInstanceID() : RandomSeed);
            InitRandom();
        }

        public List<(int CellType, int SpecialType)> GetLastCreatedPiece()
        {
            return m_LastCreatedPiece;
        }

        public List<(int CellType, int SpecialType)> GetLastDestroyedPiece()
        {
            return m_LastDestroyedPiece;
        }

        public List<(int CellType, int SpecialType)> GetLastSeenCreatedPiece()
        {
            return m_LastSeenCreatedPiece;
        }

        public List<(int CellType, int SpecialType)> GetLastSeenDestroyedPiece()
        {
            return m_LastSeenDestroyedPiece;
        }


        public void ClearLastPieceLog()
        {
            m_LastCreatedPiece.Clear();
            m_LastDestroyedPiece.Clear();
        }

        public void ClearLastSeenPieceLog()
        {
            m_LastSeenCreatedPiece.Clear();
            m_LastSeenDestroyedPiece.Clear();
        }



        public void ClearSpecialEffects()
        {
            m_SpecialEffects.Clear();
        }

        public override BoardSize GetMaxBoardSize()
        {
            return new BoardSize
            {
                Rows = MaxRows,
                Columns = MaxColumns,
                NumCellTypes = NumCellTypes,
            };
        }

        public override BoardSize GetCurrentBoardSize()
        {
            return m_CurrentBoardSize;
        }

        /// <summary>
        /// Change the board size to a random size between the min and max rows and columns. This is
        /// cached so that the size is consistent until it is updated.
        /// This is just for an example; you can change your board size however you want.
        /// </summary>
        public void UpdateCurrentBoardSize()
        {
            // var newRows = m_Random.Next(MinRows, MaxRows + 1);
            // var newCols = m_Random.Next(MinColumns, MaxColumns + 1);
            // m_CurrentBoardSize.Rows = newRows;
            // m_CurrentBoardSize.Columns = newCols;
        }

        public override bool MakeMove(Move move)
        {
            ClearLastPieceLog();

            var originalValue = m_Cells[move.Column, move.Row];
            var (otherRow, otherCol) = move.OtherCell();
            var destinationValue = m_Cells[otherCol, otherRow];

            // Check if the move is a special match
            if ((PieceType)destinationValue.SpecialType == PieceType.RainbowPiece)
            {
                m_Cells[move.Column, move.Row] = (k_EmptyCell, 0);
                m_Cells[otherCol, otherRow] = (k_EmptyCell, 0);

                m_SpecialEffects.Add(new SpecialEffect
                { 
                    CellType = originalValue.CellType,
                    SpecialType = (PieceType)destinationValue.SpecialType,
                    Row = otherRow,
                    Column = otherCol
                });

                return true;
            }
            else if ((PieceType)originalValue.SpecialType == PieceType.RainbowPiece)
            {
                m_Cells[move.Column, move.Row] = (k_EmptyCell, 0);
                m_Cells[otherCol, otherRow] = (k_EmptyCell, 0);

                m_SpecialEffects.Add(new SpecialEffect
                {
                    CellType = destinationValue.CellType,
                    SpecialType = (PieceType)originalValue.SpecialType,
                    Row = otherRow,
                    Column = otherCol
                });

                return true;
            }

            // else if ((PieceType)destinationValue.SpecialType == PieceType.RocketPiece)
            // {
            //     m_Cells[move.Column, move.Row] = (k_EmptyCell, 0);
            //     m_Cells[otherCol, otherRow] = (k_EmptyCell, 0);

            //     m_SpecialEffects.Add(new SpecialEffect
            //     {
            //         CellType = destinationValue.CellType,
            //         SpecialType = (PieceType)destinationValue.SpecialType,
            //         Row = otherRow,
            //         Column = otherCol
            //     });

            //     return true;
            // }
            // else if ((PieceType)originalValue.SpecialType == PieceType.RocketPiece)
            // {
            //     m_Cells[move.Column, move.Row] = (k_EmptyCell, 0);
            //     m_Cells[otherCol, otherRow] = (k_EmptyCell, 0);

            //     m_SpecialEffects.Add(new SpecialEffect
            //     {
            //         CellType = originalValue.CellType,
            //         SpecialType = (PieceType)originalValue.SpecialType,
            //         Row = otherRow,
            //         Column = otherCol
            //     });
 
            //     return true;
            // }
            m_Cells[move.Column, move.Row] = destinationValue;
            m_Cells[otherCol, otherRow] = originalValue;

            return true;
        }

        public override int GetCellType(int row, int col)
        {
            if (row >= m_CurrentBoardSize.Rows || col >= m_CurrentBoardSize.Columns)
            {
                throw new IndexOutOfRangeException();
            }
            return m_Cells[col, row].CellType;
        }

        public override int GetSpecialType(int row, int col)
        {
            if (row >= m_CurrentBoardSize.Rows || col >= m_CurrentBoardSize.Columns)
            {
                throw new IndexOutOfRangeException();
            }
            return m_Cells[col, row].SpecialType;
        }

        public override bool IsMoveValid(Move move)
        {
            var originalValue = m_Cells[move.Column, move.Row];
            var (otherRow, otherCol) = move.OtherCell();
            var destinationValue = m_Cells[otherCol, otherRow];
        
            if (originalValue.CellType == k_EmptyCell || destinationValue.CellType == k_EmptyCell)
            {
                return false;
            }

            // Check if the move is a special match (rainbow or rocket)
            if ((PieceType)destinationValue.SpecialType == PieceType.RainbowPiece ||
                (PieceType)originalValue.SpecialType == PieceType.RainbowPiece) // ||
                // (PieceType)destinationValue.SpecialType == PieceType.RocketPiece ||
                // (PieceType)originalValue.SpecialType == PieceType.RocketPiece)
            {
                return true;
            }

            // Check if there is a matchable piece when swap the board
            var _board = this.DeepCopy();
            
            _board.MakeMove(move);

            if (IsSameBoard(_board))
            {
                Destroy(_board);
                return false;
            }

            bool isValid = false;

            _board.MarkMatchedCells();

            if (_board.m_Matched[move.Column, move.Row] == true ||
                _board.m_Matched[otherCol, otherRow] == true)
            {
                isValid = true;
            }


            Destroy(_board);

            return isValid;
        }

        public Dictionary<PieceType, int> GetEmptyMatchableDictionary()
        {
            Dictionary<PieceType, int> matchablePieces = new Dictionary<PieceType, int>();
            // Initialize the matchablePieces
            foreach (PieceType pieceType in new PieceType[] { PieceType.HorizontalPiece, PieceType.VerticalPiece, PieceType.CrossPiece, 
                                                              PieceType.BombPiece, PieceType.RocketPiece, PieceType.RainbowPiece })
            {
                matchablePieces.Add(pieceType, 0);
            }

            return matchablePieces;
        }

        public Dictionary<PieceType, int> GetSpecialMatchable()
        {
            var matchablePieces = GetEmptyMatchableDictionary();
            
            foreach (var move in AllMoves())
            {
                if (!IsMoveValid(move))
                {
                    continue;
                }
                
                var _board = DeepCopy();
                _board.MarkMatchedCells();
                _board.SpawnSpecialCells();

                var _matchablePieces = SpecialMatch.GetMatchCount(_board.GetLastCreatedPiece());

                foreach (KeyValuePair<PieceType, int> pair in _matchablePieces)
                {
                    matchablePieces[pair.Key] = Math.Min(pair.Value | matchablePieces[pair.Key], 1);
                }

                _board = null;
            }

            return matchablePieces;
        }

        public Dictionary<PieceType, int> GetSpecialMatchableChance()
        {
            var matchablePieces = GetEmptyMatchableDictionary();
            
            foreach (var move in AllMoves())
            {
                if (!IsMoveValid(move))
                {
                    continue;
                }
                
                var _board = DeepCopy();
                _board.MakeMove(move);
                _board.MarkMatchedCells();
                _board.SpawnSpecialCells();

                var _matchablePieces = SpecialMatch.GetMatchCount(_board.GetLastCreatedPiece());

                foreach (KeyValuePair<PieceType, int> pair in _matchablePieces)
                {
                    matchablePieces[pair.Key] = Math.Min(pair.Value | matchablePieces[pair.Key], 1);
                }

                _board = null;
            }

            return matchablePieces;
        }


        public bool IsSameBoard(Match3Board board)
        {
            for (var i = 0; i < MaxRows; i++)
            {
                for (var j = 0; j < MaxColumns; j++)
                {
                    if (m_Cells[j, i].CellType != board.m_Cells[j, i].CellType || 
                        m_Cells[j, i].SpecialType != board.m_Cells[j, i].SpecialType)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public List<int[]> GetMatchedCells()
        {
            List<int[]> matchedCells = new List<int[]>();

            for (int row = 0; row < m_CurrentBoardSize.Rows; row++)
            {
                for (int col = 0; col < m_CurrentBoardSize.Columns; col++)
                {
                    if(m_Matched[col, row])
                    {
                        matchedCells.Add(new int[] { col, row });
                    }
                }
            }

            return matchedCells;
        }

        private bool IsCellInBounds(int col, int row)
        {
            return (row >= 0 && row < m_CurrentBoardSize.Rows) && (col >= 0 && col < m_CurrentBoardSize.Columns);
        }

        public int[] GetMidPosition(List<int[]> positions)
        {
            int[] midPosition = new int[2];
            int minRow = 100;
            int maxRow = 0;
            int minCol = 100;
            int maxCol = 0;

            foreach(int[] position in positions)
            {
                int row = position[1];
                int col = position[0];

                if(row < minRow) minRow = row;
                if(row > maxRow) maxRow = row;
                if(col < minCol) minCol = col;
                if(col > maxCol) maxCol = col;
            }

            midPosition[0] = (minCol + maxCol) / 2;
            midPosition[1] = (minRow + maxRow) / 2;

            return midPosition;
        }

        

        public bool MarkMatchedCells(int[,] cells = null)
        {
            ClearMarked();
            ClearCreatedCell();
            ClearSpecialMatchPositions();

            PieceType[] matchableBlocks = { 
                PieceType.NormalPiece, 
                PieceType.HorizontalPiece,
                PieceType.VerticalPiece,
                PieceType.BombPiece,
                PieceType.CrossPiece,
                PieceType.RocketPiece,
                // PieceType.RainbowPiece
            };

            bool madeMatch = false;
            for (var i = 0; i < m_CurrentBoardSize.Rows; i++)
            {
                for (var j = 0; j < m_CurrentBoardSize.Columns; j++)
                {
                    int cellType = m_Cells[j, i].CellType;
                    int specialType = m_Cells[j, i].SpecialType;

                    // Find the matchable positions
                    foreach(KeyValuePair<PieceType, List<int[,]>> matchCase in SpecialMatch.GetInstance().MatchCases)
                    {
                        PieceType pieceType = matchCase.Key;
                        List<int[,]> matchShapes = matchCase.Value;

                        List<int[]> matchedPositions = new List<int[]>(); // matched positions

                        foreach(int[,] shape in matchShapes)
                        {

                            int targetCellType = -1;
                            PieceType matchedType = pieceType;
                            matchedPositions.Clear();
 
                            for (var k = 0; k < shape.GetLength(0); k++)
                            {
                                for (var l = 0; l < shape.GetLength(1); l++)
                                {
                                    // Check for the exception
                                    if(!IsCellInBounds(j + l, i + k) || m_Matched[j + l, i + k] == true) {
                                        matchedType = PieceType.None;
                                        break;
                                    }
                                    if (shape[k, l] == 0) {
                                        continue;
                                    }
                                    else if (shape[k, l] == 1 && targetCellType == -1)
                                    {
                                        targetCellType = m_Cells[j + l, i + k].CellType;
                                    }


                                    // Check if the special blocks is in matchableBlocks 
                                    // if (m_Cells[j + l, i + k].CellType != cellType
                                    if (m_Cells[j + l, i + k].CellType != targetCellType
                                        || Array.IndexOf(matchableBlocks, (PieceType)m_Cells[j + l, i + k].SpecialType) == -1
                                    )
                                    {
                                        matchedType = PieceType.None;
                                        break;
                                    }

                                    matchedPositions.Add(new int[] {j + l, i + k});


                                }

                                if(matchedType == PieceType.None) break;
                            }

                            if(matchedType != PieceType.None) {

                                foreach(int[] position in matchedPositions)
                                {
                                    // Get SpecialType
                                    PieceType _pieceType = (PieceType)m_Cells[position[0], position[1]].SpecialType;
                                    int _cellType = m_Cells[position[0], position[1]].CellType;

                                    // Create special block matchings
                                    if (matchedType != PieceType.NormalPiece)
                                    {
                                        int[] midPosition = GetMidPosition(matchedPositions);
                                        m_CreatedCells[midPosition[0], midPosition[1]] = (cellType, (int)matchedType);    
                                    }

                                    if (_pieceType == PieceType.HorizontalPiece || 
                                        _pieceType == PieceType.VerticalPiece ||
                                        _pieceType == PieceType.CrossPiece || 
                                        _pieceType == PieceType.BombPiece ||
                                        _pieceType == PieceType.RocketPiece ||
                                        _pieceType == PieceType.RainbowPiece)
                                    {
                                        m_SpecialEffects.Add(new SpecialEffect(position[0], position[1], (PieceType)_pieceType, _cellType));
                                        m_SpecialMatchPositions.Add((_pieceType, matchedPositions));
                                    }
                            
     
                                    m_Matched[position[0], position[1]] = true;
                                    madeMatch = true;
                                }
                            }
                        }
                    }
                }
            }

            return madeMatch;
        }

        /// <summary>
        /// Sets cells that are matched to the empty cell, and returns the score earned.
        /// </summary>
        /// <returns></returns>
        public int ClearMatchedCells()
        {
            int pointsEarned = 0;
            for (var i = 0; i < m_CurrentBoardSize.Rows; i++)
            {
                for (var j = 0; j < m_CurrentBoardSize.Columns; j++)
                {
                    if (m_Matched[j, i])
                    {
                        var specialType = GetSpecialType(i, j);
                        pointsEarned += SpecialMatch.GetInstance().GetCreateScore((PieceType)specialType);
                        m_Cells[j, i] = (k_EmptyCell, 0);
                    }
                }
            }

            ClearMarked();
            return pointsEarned;
        }

        public void SpawnSpecialCells()
        {
            for (var i = 0; i < m_CurrentBoardSize.Rows; i++)
            {
                for (var j = 0; j < m_CurrentBoardSize.Columns; j++)
                {
                    if (m_CreatedCells[j, i].CellType != k_EmptyCell)
                    {
                        m_LastCreatedPiece.Add((m_CreatedCells[j, i].CellType, m_CreatedCells[j, i].SpecialType));
                        m_LastSeenCreatedPiece.Add((m_CreatedCells[j, i].CellType, m_CreatedCells[j, i].SpecialType));
                        
                        m_Cells[j, i] = m_CreatedCells[j, i];
                    }
                }
            }
        }

        public bool DropCells()
        {
            int generatedCellCount = 0;
            var madeChanges = false;
            // Gravity is applied in the negative row direction
            for (var j = 0; j < m_CurrentBoardSize.Columns; j++)
            {
                var writeIndex = 0;
                for (var readIndex = 0; readIndex < m_CurrentBoardSize.Rows; readIndex++)
                {
                    m_Cells[j, writeIndex] = m_Cells[j, readIndex];
                    if (m_Cells[j, readIndex].CellType != k_EmptyCell)
                    {
                        writeIndex++;
                    }
                }

                // Fill in empties at the end
                for (; writeIndex < m_CurrentBoardSize.Rows; writeIndex++)
                {
                    madeChanges = true;
                    generatedCellCount++;
                    m_Cells[j, writeIndex] = (k_EmptyCell, 0);
                }
            }

            return madeChanges;
        }

        public bool FillFromAbove()
        {
            bool madeChanges = false;
            for (var i = 0; i < m_CurrentBoardSize.Rows; i++)
            {
                for (var j = 0; j < m_CurrentBoardSize.Columns; j++)
                {
                    if (m_Cells[j, i].CellType == k_EmptyCell)
                    {
                        madeChanges = true;
                        m_Cells[j, i] = (GetRandomCellType(), (int)PieceType.NormalPiece);
                    }
                }
            }

            return madeChanges;
        }

        public (int, int)[,] Cells
        {
            get { return m_Cells; }
        }

        public bool[,] Matched
        {
            get { return m_Matched; }
        }

        // Initialize the board to random values.
        public void InitRandom()
        {
            for (var i = 0; i < MaxRows; i++)
            {
                for (var j = 0; j < MaxColumns; j++)
                {
                    m_Cells[j, i] = (GetRandomCellType(), (int)PieceType.NormalPiece);
                }
            }
        }

        public PieceType IsMoveSpecialMatch(Move move)
        {
            Match3Board tmpBoard = this.DeepCopy();
            tmpBoard.MakeMove(move);
            tmpBoard.ClearMatchedCells();

            tmpBoard.SpawnSpecialCells();

            var createdPieces = SpecialMatch.GetMatchCount(this.GetLastCreatedPiece());
            foreach (var (type, count) in createdPieces)
            {
                return type;
            }
            return PieceType.NormalPiece;
        }


        public void ExecuteSpecialEffect()
        {
            foreach (SpecialEffect specialEffect in m_SpecialEffects)
            {
                int row = specialEffect.Row;
                int column = specialEffect.Column;
                int cellType = specialEffect.CellType;

                switch(specialEffect.SpecialType)
                {
                    case PieceType.HorizontalPiece:

                        if (!ParameterManagerSingleton.GetInstance().IsSimpleSpecialEffectMode())
                        {
                            for (var i = 0; i < MaxColumns; i++)
                            {
                                if (!IsCellInBounds(i, row)) continue;

                                m_Matched[i, row] = true;
                                m_Cells[i, row] = (k_EmptyCell, 0);
                            }
                        }
                        else
                        {
                            for (var i = column - 2; i <= column + 2; i++)
                            {
                                if (!IsCellInBounds(i, row)) continue;

                                m_Matched[i, row] = true;
                                m_Cells[i, row] = (k_EmptyCell, 0);
                            }
                        }

                        break;
                    case PieceType.VerticalPiece:

                        if (!ParameterManagerSingleton.GetInstance().IsSimpleSpecialEffectMode())
                        {
                            for (var i = 0; i < MaxRows; i++)
                            {
                                if (!IsCellInBounds(column, i)) continue;
                                m_Matched[column, i] = true;
                                m_Cells[column, i] = (k_EmptyCell, 0);
                            }
                        }
                        else
                        {
                             for (var i = row - 2; i <= row + 2; i++)
                            {
                                if (!IsCellInBounds(column, i)) continue;
                                m_Matched[column, i] = true;
                                m_Cells[column, i] = (k_EmptyCell, 0);
                            }
                        }

                        break;
                    case PieceType.CrossPiece:


                        if (!ParameterManagerSingleton.GetInstance().IsSimpleSpecialEffectMode())
                        {
                            // Break the diagonal blocks from the row and columnts
                            for (var i = 0; i < Math.Max(MaxColumns, MaxRows); i++)
                            {
                                if (IsCellInBounds(column - i, row - i))
                                {
                                    m_Matched[column - i, row - i] = true;
                                    m_Cells[column - i, row - i] = (k_EmptyCell, 0);
                                }
                                if (IsCellInBounds(column + i, row + i))
                                {
                                    m_Matched[column + i, row + i] = true;
                                    m_Cells[column + i, row + i] = (k_EmptyCell, 0);
                                }
                                if (IsCellInBounds(column + i, row - i))
                                {
                                    m_Matched[column + i, row - i] = true;
                                    m_Cells[column + i, row - i] = (k_EmptyCell, 0);
                                }
                                if (IsCellInBounds(column - i, row + i))
                                {
                                    m_Matched[column - i, row + i] = true;
                                    m_Cells[column - i, row + i] = (k_EmptyCell, 0);
                                }
                            }
                        }
                        else
                        {
                            for (var i = 0; i <= 1; i++)
                            {
                                if (IsCellInBounds(column - i, row - i))
                                {
                                    m_Matched[column - i, row - i] = true;
                                    m_Cells[column - i, row - i] = (k_EmptyCell, 0);
                                }
                                if (IsCellInBounds(column + i, row + i))
                                {
                                    m_Matched[column + i, row + i] = true;
                                    m_Cells[column + i, row + i] = (k_EmptyCell, 0);
                                }
                                if (IsCellInBounds(column + i, row - i))
                                {
                                    m_Matched[column + i, row - i] = true;
                                    m_Cells[column + i, row - i] = (k_EmptyCell, 0);
                                }
                                if (IsCellInBounds(column - i, row + i))
                                {
                                    m_Matched[column - i, row + i] = true;
                                    m_Cells[column - i, row + i] = (k_EmptyCell, 0);
                                }
                            }   
                        }

                        break;
                    case PieceType.BombPiece:

                        // Remove around 9 blocks from the original position
                        for (var i = -1; i <= 1; i++)
                        {
                            for (var j = -1; j <= 1; j++)
                            {
                                if (!IsCellInBounds(column + i, row + j)) continue;

                                m_Matched[column + i, row + j] = true;
                                m_Cells[column + i, row + j] = (k_EmptyCell, 0);
                            }
                        }

                        break;
                    case PieceType.RocketPiece:

                        List<int[]> sameCellTypePositions = GetCellTypePosition(cellType, true);
                        if (sameCellTypePositions.Count > 0)
                        {
                            // Get nearest block from origin

                            int[] nearestPosition = GetNearestCoordinate(sameCellTypePositions, new int[] {column, row});
                            
                            m_Matched[nearestPosition[0], nearestPosition[1]] = true;
                            m_Cells[nearestPosition[0], nearestPosition[1]] = (k_EmptyCell, 0);
                        }

                        break;
                    case PieceType.RainbowPiece:

                        List<int[]> sameCellTypePositionsRainbow = GetCellTypePosition(cellType, true);
                        List<int[]> nearestPositions = GetNearestCoordinates(sameCellTypePositionsRainbow, new int[] {column, row}, 3);

                        if (nearestPositions.Count > 0)
                        {
                            foreach (int[] position in nearestPositions)
                            {
                                m_Matched[position[0], position[1]] = true;
                                m_Cells[position[0], position[1]] = (k_EmptyCell, 0);
                            }
                        }

                        break;
                    default:
                        throw new Exception("Invalid Special Type");
                }
                m_LastSeenDestroyedPiece.Add((cellType, (int)specialEffect.SpecialType));

            }
            ClearSpecialEffects();
        }

        private int[] GetNearestCoordinate(List<int[]> coordList, int[] origin)
        {
            int[] nearestCoord = new int[2];
            int minDistance = 100;

            foreach(int[] coord in coordList)
            {
                int distance = Math.Abs(coord[0] - origin[0]) + Math.Abs(coord[1] - origin[1]);
                if(distance < minDistance)
                {
                    minDistance = distance;
                    nearestCoord = coord;
                }
            }

            return nearestCoord;
        }

        private List<int[]> GetNearestCoordinates(List<int[]> coordList, int[] origin, int N)
        {
            SortedList<int, List<int[]>> nearestCoords = new SortedList<int, List<int[]>>();

            foreach (int[] coord in coordList)
            {
                int distance = Math.Abs(coord[0] - origin[0]) + Math.Abs(coord[1] - origin[1]);

                if (nearestCoords.ContainsKey(distance))
                {
                    nearestCoords[distance].Add(coord);
                }
                else
                {
                    nearestCoords.Add(distance, new List<int[]>() { coord });
                }
            }

            List<int[]> result = new List<int[]>();

            foreach (List<int[]> coords in nearestCoords.Values)
            {
                if (result.Count + coords.Count <= N)
                {
                    result.AddRange(coords);
                }
                else
                {
                    int remaining = N - result.Count;
                    result.AddRange(coords.GetRange(0, remaining));
                    break;
                }
            }

            return result;
        }

        private List<int[]> SampleRandomCoordinate(List<int[]> coordList, int N)
        {
            List<int[]> shuffledItems = new List<int[]>(coordList);
            int n = shuffledItems.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                int[] temp = shuffledItems[k];
                shuffledItems[k] = shuffledItems[n];
                shuffledItems[n] = temp;
            }

            List<int[]> sampledItems = new List<int[]>();
            for (int i = 0; i < Mathf.Min(N, coordList.Count); i++)
            {
                sampledItems.Add(shuffledItems[i]);
            }

            return sampledItems;
        }

        // Get the list of the posititons of the same cell type
        public List<int[]> GetCellTypePosition(int cellType, bool checkMatched = false)
        {
            List<int[]> sameCellTypePositions = new List<int[]>();
    
            for (var i = 0; i < MaxRows; i++)
            {
                for (var j = 0; j < MaxColumns; j++)
                {
                    if (m_Cells[j, i].CellType == cellType)
                    {
                        if (checkMatched)
                        {
                            if (!m_Matched[j, i])
                            {
                                sameCellTypePositions.Add(new int[] { j, i });
                            }
                        }
                        else
                        {
                            sameCellTypePositions.Add(new int[] { j, i });
                        }
                    }
                }
            }
            
            return sameCellTypePositions;
        }

        public void InitSettled()
        {
            InitRandom();
            while (true)
            {
                var anyMatched = MarkMatchedCells();
                if (!anyMatched)
                {
                    return;
                }
                ClearMatchedCells();

                ExecuteSpecialEffect();
                // Create the spcial blocks to the board (before dropping)
                SpawnSpecialCells();

                DropCells();
                FillFromAbove();
            }
        }

        public void ClearMarked()
        {
            for (var i = 0; i < MaxRows; i++)
            {
                for (var j = 0; j < MaxColumns; j++)
                {
                    m_Matched[j, i] = false;
                }
            }
        }

        public void ClearCreatedCell()
        {
            for (var i = 0; i < MaxRows; i++)
            {
                for (var j = 0; j < MaxColumns; j++)
                {
                    m_CreatedCells[j, i] = (-1, (int)PieceType.None);;
                }
            }
        }



        int GetRandomCellType()
        {
            return m_Random.Next(0, NumCellTypes);
        }


        int GetRandomSpecialType()
        {
            return m_Random.Next((int)PieceType.NormalPiece, (int)PieceType.RainbowPiece);
        }

        public Match3Board DeepCopy()
        {
            Match3Board board = new Match3Board();
            
            board.MaxColumns = this.MaxColumns;
            board.MaxRows = this.MaxRows;
            board.MinColumns = this.MinColumns;
            board.MinRows = this.MinRows;
            board.RandomSeed = this.RandomSeed;
            board.NumCellTypes = this.NumCellTypes;
            board.m_Random = new System.Random(this.RandomSeed);
            board.Awake();
            
            board.m_Cells = ((int CellType, int SpecialType)[,])m_Cells.Clone();
            board.m_Matched = (bool[,])m_Matched.Clone();

            var boardsize = this.GetCurrentBoardSize();
            board.m_CurrentBoardSize = new BoardSize
            {
                Rows = boardsize.Rows,
                Columns = boardsize.Columns,
                NumCellTypes = boardsize.NumCellTypes,
            };
            
            return board;
        }

        public bool HasEmptyCell()
        {
            int[] emptyCell = GetEmptyCell();
            return emptyCell != null;
        }

        public int[] GetEmptyCell()
        {
            for (var i = 0; i < MaxRows; i++)
            {
                for (var j = 0; j < MaxColumns; j++)
                {
                    if (m_Cells[j, i].CellType == k_EmptyCell)
                    {
                        return new int[] {j, i};
                    }
                }
            }
            return null;
        }

        public int GetEmptyCellCount()
        {
            int count = 0;
            for (var i = 0; i < MaxRows; i++)
            {
                for (var j = 0; j < MaxColumns; j++)
                {
                    if (m_Cells[j, i].CellType == k_EmptyCell)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public void SpawnRandomBlock()
        {
            int[] emptyCell = GetEmptyCell();
            if (emptyCell == null) return;

            int cellType = GetRandomCellType();
            m_Cells[emptyCell[0], emptyCell[1]] = (cellType, (int)PieceType.NormalPiece);
        }

        public int[] SpawnColoredBlock(int cellType)
        {
            int[] emptyCell = GetEmptyCell();
            if (emptyCell == null) return null;

            m_Cells[emptyCell[0], emptyCell[1]] = (cellType, (int)PieceType.NormalPiece);

            return emptyCell;
        }


        public int EvalMovePoints(Move move)
        {
            var _board = this.DeepCopy();
            _board.MakeMove(move);
            _board.MarkMatchedCells();

            var pointsEarned = _board.ClearMatchedCells();
            _board.ExecuteSpecialEffect();
            _board.SpawnSpecialCells();


            int createdPoints = 0, destroyedPoints = 0;

            var createdPieces = _board.GetLastCreatedPiece();
            var destroyedPieces = _board.GetLastDestroyedPiece();

            foreach (var piece in createdPieces)
            {
                createdPoints += SpecialMatch.GetInstance().GetCreateScore((PieceType)piece.SpecialType);
            }

            foreach (var piece in destroyedPieces)
            {
                destroyedPoints += SpecialMatch.GetInstance().GetDestroyScore((PieceType)piece.SpecialType);
            }

            int points = createdPoints + destroyedPoints;
            
            return points;
        }
        private void ClearSpecialMatchPositions()
        {
            m_SpecialMatchPositions.Clear();
        }

        public List<(PieceType SpecialType, List<int[]> Positions)> GetSpecialMatchPositions()
        {
            return m_SpecialMatchPositions;
        }

        public void SaveTo(string path)
        {
            SerializableBoard board = new SerializableBoard(this);

            IFormatter formatter = new BinaryFormatter();
            Stream streamFileWrite = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(streamFileWrite, board);
            streamFileWrite.Close();
        }

        private void OnLoadComplete(byte[] assetBundleData)
        {       
            if (assetBundleData != null)
            {
                // Create a memory stream from the downloaded data
                MemoryStream stream = new MemoryStream(assetBundleData);
                IFormatter formatter = new BinaryFormatter();
                SerializableBoard loadedBoard = (SerializableBoard)formatter.Deserialize(stream);
                m_Cells = ((int CellType, int SpecialType)[,])loadedBoard.m_Cells.Clone();
            }
        }

        public bool LoadCells((int CellType, int SpecialType)[,] cells)
        {
            m_Cells = cells;
            return true;
        }
        /*
        public bool LoadFrom(string path)
        {
            bool IsSuccess = false;
            Debug.Log("LoadFrom: " + path);

            // try
            // {
                // If path includes http
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
                m_Cells = ((int CellType, int SpecialType)[,])loadedBoard.m_Cells.Clone();
                IsSuccess = true;
            }



            // }
            // catch (Exception e)
            // {
            //     Debug.Log(e);
            // }
            return IsSuccess;
        }
        */
    }



}
