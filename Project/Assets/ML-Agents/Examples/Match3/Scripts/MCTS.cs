using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Integrations.Match3;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.MLAgentsExamples
{
    public enum SimulationType
    {
        Generator = 0,
        Solver = 1,
    }


    public class Node
    {
        public int visits;
        public int depth;
        public int playerActionCount;
        public float score;
        public List<Node> children;
        public Node parent;
        public Match3Board board;
        public SimulationType simulationType;
        Dictionary<PieceType, int> matchablePieces;
        public List<(int CellType, int SpecialType)> createdPieces;
        public bool IsSimulated;
        public SkillKnowledge playerKnowledge;

        public Node(int depth, 
                    int visits,
                    int playerActionCount,
                    float score,
                    List<Node> children,
                    Node parent,
                    Match3Board board,
                    SimulationType simulationType,
                    SkillKnowledge playerKnowledge = null)
        {
            this.depth = depth;
            this.visits = visits;
            this.playerActionCount = playerActionCount;
            this.score = score;
            this.children = children;
            this.parent = parent;
            this.board = board;
            this.simulationType = simulationType;
            this.IsSimulated = false;
            this.playerKnowledge = playerKnowledge;
        }
        ~Node()
        {
            board = null;
            parent = null;
            children = null;
        }


        public Dictionary<PieceType, int> GetMatchableBlockCount()
        {
            if (matchablePieces != null) return matchablePieces;

            matchablePieces = board.GetSpecialMatchable();

            return matchablePieces;

        }

        public override string ToString()
        {
            var matchables = GetMatchableBlockCount();
            string output = "";
            foreach (var match in matchables)
            {
                output += $"{match.Key}: {match.Value}";
            }
            return output;
        }

    }

    public class MCTS
    {
        private static MCTS _Instance = null;

        public static MCTS Instance { get {
            if (_Instance == null)
            {
                _Instance = new MCTS();
            }
            return _Instance;
        }}

        private Match3Board simulator;
        private Node rootNode;
        private Node currentNode;


        private float BestBoardScore;
        private Match3Board BestBoard;
        public int numberOfChild;
        public int DepthLimit = 2;
        public int MaxPlayerDepth = 2;

        public int simulationStepLimit = 300;

        private int TargetDepth = 0;

        private int m_MaxDepth = 0;

        private bool IsChanged = false;
        private int m_ComparisonCount = 0;

        private int ExpandCount = 0;
        private int PlayerDepthLimit = 1;

        private GeneratorReward RewardMode = GeneratorReward.Score;
        private SkillKnowledge PlayerKnowledge = null;
        public Dictionary<PieceType, float> PieceScoreWeight = null;
        private float KnowledgeAlmostRatio = 1.0f;
        private bool Verbose = false;

        GameObject m_DummyBoard;

        public MCTS()
        {
            m_DummyBoard = GameObject.Find("DummyBoard").gameObject;
            InitializePieceScoreWeight();
        }

        public void InitializePieceScoreWeight()
        {
            // Reversed number of rarity
            // 0.747835102	0.823021655	0.96658129	0.538003526	0.967900742	0.956657686
            PieceScoreWeight = new Dictionary<PieceType, float>();
            PieceScoreWeight.Add(PieceType.HorizontalPiece, 1.0f);
            PieceScoreWeight.Add(PieceType.VerticalPiece, 1.0f);
            PieceScoreWeight.Add(PieceType.CrossPiece, 1.0f);
            PieceScoreWeight.Add(PieceType.BombPiece, 1.0f);
            PieceScoreWeight.Add(PieceType.RocketPiece, 1.0f);
            PieceScoreWeight.Add(PieceType.RainbowPiece, 1.0f);
        }

        public void PrepareRootNode()
        {
            ResetRootNode();
            currentNode = rootNode;
        }

        private void ResetRootNode()
        {  
            rootNode = new Node(0, 0, 0, 0f, new List<Node>(), null, simulator, SimulationType.Generator, PlayerKnowledge.DeepCopy());
            Expand(rootNode);
        }

        public void Search()
        {
            currentNode = rootNode;

            // Select (Find the terminal node ,tree policy)
            while (currentNode.children.Count > 0)
            {
                currentNode = SelectBestChild(currentNode);
            }

            // Expand
            // TODO 여기 수정해야함!
            if (currentNode.visits <= 1 && 
                currentNode.playerActionCount < PlayerDepthLimit) 
            {
                Expand(currentNode);
                currentNode = SelectBestChild(currentNode);
            }

            // Rollout (Default policy)
            float score = Simulate(currentNode);

            // Backpropagate
            while (currentNode != null) {
                currentNode.visits++;
                currentNode.score += score;
                
                if (currentNode.score > BestBoardScore && currentNode.depth == TargetDepth)
                {
                    BestBoardScore = currentNode.score;
                    BestBoard = currentNode.board;

                    IsChanged = true;
                    m_ComparisonCount += 1;

                }
        
                currentNode = currentNode.parent;
            }
        }

        public float FillEmpty(Match3Board board, SkillKnowledge knowledge, int playerDepthLimit = 1)
        {
            int _emptyCellCount = board.GetEmptyCellCount();
            // Print the empty cell count
            PlayerKnowledge = knowledge;

            var _board = board.DeepCopy();

            // Initialize the searching process
            simulator = _board;
            m_ComparisonCount = 0;
            ExpandCount = 0;
            m_MaxDepth = 0;
            PlayerDepthLimit = playerDepthLimit;

            // Fill Empty cells
            PrepareRootNode();
            PrepareSearch();

            for(int i = 0; i < simulationStepLimit; i++)
            {
                Search();
            }

            board.m_Cells = ((int CellType, int SpecialType)[,])BestBoard.m_Cells.Clone();

            this.rootNode = null;
            
            // Print Simulation Results
            string _log = $"[{RewardMode}:{simulationStepLimit}] ";    

            _log += "Target Depth: " + _emptyCellCount + " / " +
                "Expand Count: " + ExpandCount + " / Max Depth: " + m_MaxDepth +
              " / IsChanged: " + IsChanged + " / BestBoardScore: " + BestBoardScore;
            if (RewardMode.Equals(GeneratorReward.Knowledge))
            {
                _log += " / KnowledgeAlmostRatio: " + KnowledgeAlmostRatio.ToString();
            }
            if (Verbose) Debug.Log(_log);

            return BestBoardScore;
        }

        private void PrepareSearch()
        {
            TargetDepth = simulator.GetEmptyCellCount();
            // DepthLimit = TargetDepth + 1; // Upto solver's node

            BestBoardScore = 0.0f;

            BestBoard = simulator.DeepCopy();
            BestBoard.FillFromAbove();

            IsChanged = false;
        }


        private Node SelectBestChild(Node node) {
            float bestScore = float.MinValue;
            Node bestChild = null;

            foreach (Node child in node.children) {
                float score = child.score / child.visits + Mathf.Sqrt(2 * Mathf.Log(node.visits) / child.visits);
                if (float.IsNaN(score))
                {
                    score = 0;
                }
                if (score > bestScore) {
                    bestScore = score;
                    bestChild = child;
                }
            }

            return bestChild;
        }

        private int[] GetRandomIntArray(int maxVal)
        {
            int[] randomArray = new int[maxVal];
            System.Random random = new System.Random();

            for (int i = 0; i < randomArray.Length; i++) {
                randomArray[i] = i;
            }

            for (int i = randomArray.Length - 1; i > 0; i--) {
                int j = random.Next(i + 1);
                int temp = randomArray[i];
                randomArray[i] = randomArray[j];
                randomArray[j] = temp;
            }

            return randomArray;
        }

        private void Expand(Node node) {

            SimulationType simType = node.board.HasEmptyCell() ? SimulationType.Generator : SimulationType.Solver;
            Node tmpChild = null;
            Match3Board tmpBoard = null;

            // if (node.depth > DepthLimit)
            // {
            //     return;
            // }

            if (node.depth > m_MaxDepth)
            {
                m_MaxDepth = node.depth;
            }

            switch(simType)
            {
                case SimulationType.Generator:
                    
                    int[] randomArray = GetRandomIntArray(node.board.NumCellTypes);

                    for (int i = 0; i < randomArray.Length; i++)
                    {
  
                        tmpBoard = node.board.DeepCopy();
                        int[] spawnedPos = tmpBoard.SpawnColoredBlock(randomArray[i]); // Get spawned block position here

                        // Check if the PCG agent make a match case which a player didn't make
                        Match3Board _tmpBoard = tmpBoard.DeepCopy();

                        bool makeNode = true;

                        // Check if the spawned position made a special match
                        bool madeMatch = _tmpBoard.MarkMatchedCells();
                        if (madeMatch)
                        {
                            List<(PieceType SpecialType, List<int[]> Positions)> specialMatches = _tmpBoard.GetSpecialMatchPositions();
                            foreach ((int SpecialType, List<int[]> Positions) specialMatch in specialMatches)
                            {
                                foreach (int[] pos in specialMatch.Positions)
                                {
                                    if (pos[0] == spawnedPos[0] && pos[1] == spawnedPos[1])
                                    {
                                        makeNode = false;
                                        break;
                                    }
                                }
                                if (!makeNode) break;
                            }

                        }

                        _tmpBoard = null;
                        

                        if (makeNode)
                        {
                            tmpChild = new Node(node.depth + 1, 0, node.playerActionCount, 0f, new List<Node>(), node, tmpBoard, SimulationType.Generator, node.playerKnowledge.DeepCopy());
                            node.children.Add(tmpChild);
                            ExpandCount += 1;
                        }
                    }

                    break;
                case SimulationType.Solver:
                    tmpBoard = node.board.DeepCopy();

                    Move move = GreedyMatch3Solver.GetAction(tmpBoard);
                    tmpBoard.MakeMove(move);

                    tmpBoard.MarkMatchedCells();
                    tmpBoard.ClearMatchedCells();
                    tmpBoard.SpawnSpecialCells();

                    // Record the matched blocks
                    var createdPieces = tmpBoard.GetLastCreatedPiece();
                    List<(int CellType, int SpecialType)> _createdPieces = createdPieces.ConvertAll(item => (item.CellType, item.SpecialType));
                    tmpBoard.ClearLastPieceLog();

                    tmpBoard.ExecuteSpecialEffect();
                    tmpBoard.DropCells();

                    tmpChild = new Node(node.depth + 1, 0, node.playerActionCount + 1, 0f, new List<Node>(), node, tmpBoard, SimulationType.Solver, node.playerKnowledge.DeepCopy());
                    tmpChild.createdPieces = _createdPieces;

                    node.children.Add(tmpChild);
                    
                    ExpandCount += 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        private float Simulate(Node node)
        {

            float score = 0f;
            bool hasMatched;

            switch (node.simulationType)
            {
                case SimulationType.Generator:

                    // 새로 만들어진 블럭들에게서 점수를 구함
                    hasMatched = node.board.MarkMatchedCells();
                    if (hasMatched)
                    {
                        score = -0.001f;
                    }
                    break;


                case SimulationType.Solver:  
                    switch(RewardMode)
                    {
                        case GeneratorReward.Score:
                            if (!node.IsSimulated)
                            {
                                foreach ((int CellType, int SpecialType) piece in node.createdPieces)
                                {
                                    score += (float)Math.Pow(PieceScoreWeight[(PieceType)piece.SpecialType], 1);
                                    node.playerKnowledge.IncreaseMatchCount((PieceType)piece.SpecialType);
                                }

                                // score += node.createdPieces.Count;
                                node.IsSimulated = true;
                            }
                            break;
                        case GeneratorReward.Knowledge:
                            if (!node.IsSimulated)
                            {
                                foreach ((int CellType, int SpecialType) piece in node.createdPieces)
                                {
                                    bool isReached = node.playerKnowledge.IsMatchCountAlmostReachedTarget((PieceType)piece.SpecialType, KnowledgeAlmostRatio);
                                    if (!isReached) // Have to learn
                                    {
                                        score += (float)Math.Pow(PieceScoreWeight[(PieceType)piece.SpecialType], 1);
                                        node.playerKnowledge.IncreaseMatchCount((PieceType)piece.SpecialType);
                                    }
                                }

                                node.IsSimulated = true;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();

            }

            return score;
        }

        private bool IsTerminal(Node node)
        {
            // TODO Add node depth for player simulating
            return HasValidMoves(node.board.ValidMoves()) || node.playerActionCount >= 1;
        }


        private SimulationType GetSimulationType(Match3Board baord) 
        {
            // Return the simulation type whether the empty space is exist in the board
            if (baord.HasEmptyCell())
            {
                return SimulationType.Generator;
            }
            else
            {
                return SimulationType.Solver;
            }
        }

         
        bool HasValidMoves(IEnumerable<Move> board)
        {
            foreach (var unused in board)
            {
                return true;
            }

            return false;
        }

        public void SetRewardMode(GeneratorReward mode)
        {
            this.RewardMode = mode;
        }
        
        
        public void SetSimulationLimit(int limit)
        {
            this.simulationStepLimit = limit;
        }

        public void SetKnowledgeAlmostRatio(float ratio)
        {
            this.KnowledgeAlmostRatio = ratio;
        }

        public void SetVerbose(bool verbose)
        {
            this.Verbose = verbose;
        }

        public int GetComparisonCount()
        {
            return m_ComparisonCount;
        }

    }
}