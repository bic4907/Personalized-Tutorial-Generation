using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Integrations.Match3;
using UnityEditor;


namespace Unity.MLAgentsExamples
{


    public class BoardPCGAgent : Agent
    {
        [HideInInspector]
        public Match3Board Board;

        public float MoveTime = 0.0f;
        public int MaxMoves = 500;

        State m_CurrentState = State.WaitForMove;
        float m_TimeUntilMove;
        private int m_MovesMade;
        private ModelOverrider m_ModelOverrider;

        public bool useForcedFast = true;

        public GeneratorType generatorType = GeneratorType.MCTS;

        public GeneratorReward generatorRewardType = GeneratorReward.Score;

        public static PieceType[] PieceLogOrder = new PieceType[6] {PieceType.HorizontalPiece, PieceType.VerticalPiece, PieceType.CrossPiece, PieceType.RocketPiece, PieceType.BombPiece, PieceType.RainbowPiece};

        private string m_uuid = System.Guid.NewGuid().ToString().Substring(0, 8);

        private PCGStepLog m_Logger;

        public int PlayerNumber = -1;

        public int MCTS_Simulation = 300;
        public int SamplingNum = 10;
        public int EvoluationNum = 100;
        private SkillKnowledge m_SkillKnowledge;
        private SkillKnowledge m_ManualSkillKnowledge;

        private const float k_RewardMultiplier = 0.01f;

        public int CurrentEpisodeCount = 0;
        public int TotalStepCount = 0;
        public int CurrentStepCount = 0;
        public int TargetEpisodeCount = -1;
        public int SettleCount = 0;
        public int ChangedCount = 0;
        public int NonChangedCount = 0;

        public float KnowledgeAlmostRatio = 0.75f;
        public int KnowledgeReachStep = -1;
        public int KnowledgeQ1ReachStep = -1; // 3/4 percentqage of the target
        public int KnowledgeQ2ReachStep = -1; // 3/4 percentqage of the target
        public int KnowledgeQ3ReachStep = -1; // 3/4 percentqage of the target
        public int PlayerDepthLimit = 1;
        public float GreedyActionRatio = 1.0f;

        public List<int> ComparisonCounts;
        public List<float> GeneratingRuntimes;
        public List<float> GeneratingScores;
        public bool SaveFirebaseLog = false;
        private FirebaseLogger m_FirebaseLogger;

        [Header("")]
        public AgentType agentType = AgentType.Agent;
        public MouseInteraction m_mouseInput;

        private System.Random m_Random = new System.Random();

        private List<int> m_EmptyCellCounts = new List<int>();

        protected override void Awake()
        {
            base.Awake();
            Board = GetComponent<Match3Board>();
            m_ModelOverrider = GetComponent<ModelOverrider>();
            m_Logger = new PCGStepLog();

            if (SaveFirebaseLog)
            {
                // Add FirebaseLogger component in this game objct
                if (this.gameObject.GetComponent<FirebaseLogger>() == null)
                {
                    m_FirebaseLogger = this.gameObject.AddComponent<FirebaseLogger>();
                    m_FirebaseLogger.SetUUID(m_uuid);
                }
            }

            // Parsing the augments
            if(ParameterManagerSingleton.GetInstance().HasParam("targetPlayer"))
            {
                PlayerNumber = Convert.ToInt32(ParameterManagerSingleton.GetInstance().GetParam("targetPlayer"));
            }
            if(ParameterManagerSingleton.GetInstance().HasParam("method"))
            {
                string _method = Convert.ToString(ParameterManagerSingleton.GetInstance().GetParam("method"));

                switch (_method)
                {
                    case "mcts":
                        generatorType = GeneratorType.MCTS;
                        break;
                    case "random":
                        generatorType = GeneratorType.Random;
                        break;
                    case "sampling":
                        generatorType = GeneratorType.Sampling;
                        break;
                    case "ga":
                        generatorType = GeneratorType.GA;
                        break;
                }

            }
            if(ParameterManagerSingleton.GetInstance().HasParam("objective"))
            {
                string _objective = Convert.ToString(ParameterManagerSingleton.GetInstance().GetParam("objective"));

                switch (_objective)
                {
                    case "score":
                        generatorRewardType = GeneratorReward.Score;
                        break;
                    case "knowledge":
                        generatorRewardType = GeneratorReward.Knowledge;
                        break;
                }
            }
            if(ParameterManagerSingleton.GetInstance().HasParam("mctsSimulation"))
            {
                MCTS_Simulation = Convert.ToInt32(ParameterManagerSingleton.GetInstance().GetParam("mctsSimulation"));
            }
            if(ParameterManagerSingleton.GetInstance().HasParam("targetEpisodeCount"))
            {
                TargetEpisodeCount = Convert.ToInt32(ParameterManagerSingleton.GetInstance().GetParam("targetEpisodeCount"));
            }
            if(ParameterManagerSingleton.GetInstance().HasParam("knowledgeAlmostRatio"))
            {
                KnowledgeAlmostRatio = (float)Convert.ToDouble(ParameterManagerSingleton.GetInstance().GetParam("knowledgeAlmostRatio"));
            }
            if(ParameterManagerSingleton.GetInstance().HasParam("playerDepth"))
            {
                PlayerDepthLimit = Convert.ToInt32(ParameterManagerSingleton.GetInstance().GetParam("playerDepth"));
            }
            if(ParameterManagerSingleton.GetInstance().HasParam("samplingNum"))
            {
                SamplingNum = Convert.ToInt32(ParameterManagerSingleton.GetInstance().GetParam("samplingNum"));
            }
            if(ParameterManagerSingleton.GetInstance().HasParam("greedyActionRatio"))
            {
                GreedyActionRatio = (float)Convert.ToDouble(ParameterManagerSingleton.GetInstance().GetParam("greedyActionRatio"));
            }
            if(ParameterManagerSingleton.GetInstance().HasParam("evolutionNum"))
            {
                EvoluationNum = Convert.ToInt32(ParameterManagerSingleton.GetInstance().GetParam("evolutionNum"));
            }


            m_SkillKnowledge = SkillKnowledgeExperimentSingleton.Instance.GetSkillKnowledge(PlayerNumber);
            m_ManualSkillKnowledge = new SkillKnowledge();

            ComparisonCounts = new List<int>();
            GeneratingRuntimes = new List<float>();
            GeneratingScores = new List<float>();
        }

        public override void OnEpisodeBegin()
        {
            base.OnEpisodeBegin();

            Board.UpdateCurrentBoardSize();
            Board.InitSettled();
            m_CurrentState = State.FindMatches;
            m_TimeUntilMove = MoveTime;
            m_MovesMade = 0;

            if (m_Logger != null && CurrentEpisodeCount != 0)
            {
                RecordResult();
            }

            m_Logger = new PCGStepLog();
            m_SkillKnowledge = SkillKnowledgeExperimentSingleton.Instance.GetSkillKnowledge(PlayerNumber);

            CurrentEpisodeCount += 1;
            CurrentStepCount = 0;
            SettleCount = 0;
            ChangedCount = 0;

            if(TargetEpisodeCount != -1 && CurrentEpisodeCount > TargetEpisodeCount)
            {
                # if UnityEditor
                    UnityEditor.EditorApplication.isPlaying = false;
                # else
                    Application.Quit();
                #endif
            }

            ResetKnowledgeReach();
            ComparisonCounts.Clear();
            GeneratingRuntimes.Clear();
        }

        private void OnPlayerAction()
        {
            if (SaveFirebaseLog && CurrentEpisodeCount != 0)
            {
                FirebaseLog.LearningLog log = new FirebaseLog.LearningLog();
                log.EpisodeCount = CurrentEpisodeCount;
                log.EpisodeStepCount = CurrentStepCount;
                log.TotalStepCount = TotalStepCount;
                log.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                log.InstanceUUID = m_uuid;
                log.SkillKnowledge = m_ManualSkillKnowledge;
                m_FirebaseLogger.Post(log);
            }

        }

        private void FixedUpdate()
        {
            MCTS.Instance.SetRewardMode(generatorRewardType);
            MCTS.Instance.SetSimulationLimit(MCTS_Simulation);
            MCTS.Instance.SetKnowledgeAlmostRatio(KnowledgeAlmostRatio);

            GeneticAlgorithm.Instance.SetRewardMode(generatorRewardType);

            // Make a move every step if we're training, or we're overriding models in CI.
            var useFast = Academy.Instance.IsCommunicatorOn || (m_ModelOverrider != null && m_ModelOverrider.HasOverrides);
            if (useFast)
            {
                MCTS.Instance.SetVerbose(false);
            }

            if (useFast || useForcedFast)
            {
                FastUpdate();
            }
            else
            {
                AnimatedUpdate();
            }

            // We can't use the normal MaxSteps system to decide when to end an episode,
            // since different agents will make moves at different frequencies (depending on the number of
            // chained moves). So track a number of moves per Agent and manually interrupt the episode.
            if (CurrentStepCount >= MaxMoves)
            {
                EpisodeInterrupted();
            }


        }

        void FastUpdate()
        {
            while (true)
            {
                var hasMatched = Board.MarkMatchedCells();
                if (!hasMatched)
                {
                    if (Board.GetEmptyCellCount() > 0)
                    {

                    }
                    else
                    {
                        break;
                    }
                }
                var pointsEarned = Board.ClearMatchedCells();
                AddReward(k_RewardMultiplier * pointsEarned);

                Board.SpawnSpecialCells();

                var createdPieces = SpecialMatch.GetMatchCount(Board.GetLastCreatedPiece());
                foreach (var (type, count) in createdPieces)
                {
                    m_SkillKnowledge.IncreaseMatchCount(type, count);
                    m_ManualSkillKnowledge.IncreaseMatchCount(type, count);
                }
                Board.ExecuteSpecialEffect();

                Board.DropCells();

                var startTime = Time.realtimeSinceStartup;

                float score = 0.0f;

                switch(generatorType)
                {
                    case GeneratorType.Random:
                        Board.FillFromAbove();
                        break;
                    case GeneratorType.MCTS:
                        score = MCTS.Instance.FillEmpty(Board, m_SkillKnowledge, PlayerDepthLimit);
                        break;
                    case GeneratorType.Sampling:
                        score = BoardSampler.Instance.FillEmpty(Board, m_SkillKnowledge, SamplingNum);
                        break;
                    case GeneratorType.GA:
                        score = GeneticAlgorithm.Instance.FillEmpty(Board, m_SkillKnowledge, EvoluationNum);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                GeneratingScores.Add(Math.Max(score, 0.0f));
                GeneratingRuntimes.Add(Time.realtimeSinceStartup - startTime);
            }


            bool isBoardSettled = false;
            while (!HasValidMoves())
            {
                Board.InitSettled();
                isBoardSettled = true;
            }

            if (isBoardSettled)
            {
                SettleCount += 1;
            }

            CheckKnowledgeReach();

            // Simulate the board with greedy action
            Move move = new Move();
            if (m_Random.NextDouble() < GreedyActionRatio)
            {
                move = GreedyMatch3Solver.GetAction(Board);
            }
            else
            {
                move = RandomMatch3Solver.GetAction(Board);
            }
            Board.MakeMove(move);

            CurrentStepCount += 1;
            TotalStepCount += 1;

            OnPlayerAction();

            m_MovesMade++;
        }

        void AnimatedUpdate()
        {
            var startTime = Time.realtimeSinceStartup;
            m_TimeUntilMove -= Time.deltaTime;
            if (m_TimeUntilMove > 0.0f)
            {
                return;
            }

            m_TimeUntilMove = MoveTime;

            State nextState;

            switch (m_CurrentState)
            {
                case State.FindMatches:
                    var hasMatched = Board.MarkMatchedCells();
                    nextState = hasMatched ? State.ClearMatched : State.WaitForMove;
                    if (nextState == State.WaitForMove)
                    {
                        m_MovesMade++;
                    }
                    break;
                case State.ClearMatched:
                    var pointsEarned = Board.ClearMatchedCells();
                    AddReward(k_RewardMultiplier * pointsEarned);

                    Board.SpawnSpecialCells();

                    var createdPieces = SpecialMatch.GetMatchCount(Board.GetLastCreatedPiece());
                    foreach (var (type, count) in createdPieces)
                    {
                        m_SkillKnowledge.IncreaseMatchCount(type, count);
                        m_ManualSkillKnowledge.IncreaseMatchCount(type, count);
                    }
                    Board.ExecuteSpecialEffect();

                    nextState = State.Drop;
                    break;
                case State.Drop:
                    Board.DropCells();
                    nextState = State.FillEmpty;
                    break;
                case State.FillEmpty:
                    startTime = Time.realtimeSinceStartup;

                    float score = 0.0f;

                    int emptyCellCount = Board.GetEmptyCellCount();
                    m_EmptyCellCounts.Add(emptyCellCount);

                    // Print the mean and std

                    Debug.Log("Mean Empty Cell Count: " + m_EmptyCellCounts.Average());
                    Debug.Log("Std Empty Cell Count: " + CalculateStandardDeviation(m_EmptyCellCounts));

                    switch(generatorType)
                    {
                        case GeneratorType.Random:
                            Board.FillFromAbove();
                            break;
                        case GeneratorType.MCTS:
                            score = MCTS.Instance.FillEmpty(Board, m_SkillKnowledge, PlayerDepthLimit);
                            break;
                        case GeneratorType.Sampling:
                            score = BoardSampler.Instance.FillEmpty(Board, m_SkillKnowledge, SamplingNum);
                            break;
                        case GeneratorType.GA:
                            score = GeneticAlgorithm.Instance.FillEmpty(Board, m_SkillKnowledge, EvoluationNum);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    GeneratingScores.Add(Math.Max(score, 0.0f));
                    GeneratingRuntimes.Add(Time.realtimeSinceStartup - startTime);

                    CurrentStepCount += 1;
                    TotalStepCount += 1;

                    nextState = State.FindMatches;
                    break;
                case State.WaitForMove:
                    bool isBoardSettled = false;
                    nextState = State.WaitForMove;
                    Move move = new Move();
                    while (true)
                    {
                        // Shuffle the board until we have a valid move.
                        bool hasMoves = HasValidMoves();
                        if (hasMoves)
                        {
                            break;
                        }
                        Board.InitSettled();
                        isBoardSettled = true;
                    }

                    if (isBoardSettled)
                    {
                        SettleCount += 1;
                    }

                    CheckKnowledgeReach();
                    switch(agentType)
                    {
                        case AgentType.Agent:
                            // Get rand and compare if sample random match3 solver
                            if (m_Random.NextDouble() < GreedyActionRatio)
                            {
                                move = GreedyMatch3Solver.GetAction(Board);
                            }
                            else
                            {
                                move = RandomMatch3Solver.GetAction(Board);
                            }

                            Board.MakeMove(move);
                            OnPlayerAction();

                            nextState = State.FindMatches;
                        break;
                        case AgentType.Human:
                            if(m_mouseInput.playerHadVaildAction == true)
                            {
                                move = m_mouseInput.GetMove();
                                Board.MakeMove(move);
                                OnPlayerAction();

                                nextState = State.FindMatches;

                            }
                        break;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            m_CurrentState = nextState;
        }

        public void ResetKnowledgeReach()
        {
            KnowledgeReachStep = -1;
            KnowledgeQ1ReachStep = -1;
            KnowledgeQ2ReachStep = -1;
            KnowledgeQ3ReachStep = -1;

        }


        public void CheckKnowledgeReach()
        {
            if (KnowledgeReachStep == -1 && m_SkillKnowledge.IsAllBlockReachTarget())
            {
                KnowledgeReachStep = CurrentStepCount;
            }
            if (KnowledgeQ3ReachStep == -1 && m_SkillKnowledge.IsAllBlockAlmostReachTarget(0.75f))
            {
                KnowledgeQ3ReachStep = CurrentStepCount;
            }
            if (KnowledgeQ2ReachStep == -1 && m_SkillKnowledge.IsAllBlockAlmostReachTarget(0.50f))
            {
                KnowledgeQ2ReachStep = CurrentStepCount;
            }
            if (KnowledgeQ1ReachStep == -1 && m_SkillKnowledge.IsAllBlockAlmostReachTarget(0.25f))
            {
                KnowledgeQ1ReachStep = CurrentStepCount;
            }
        }

        bool HasValidMoves()
        {
            foreach (var unused in Board.ValidMoves())
            {
                return true;
            }

            return false;
        }

        public void RecordResult()
        {
            // Make a new PCG Log file with the parameters

            m_Logger.EpisodeCount = CurrentEpisodeCount;
            m_Logger.StepCount = CurrentStepCount;
            m_Logger.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            m_Logger.SkillKnowledge = m_SkillKnowledge;
            m_Logger.InstanceUUID = m_uuid;
            m_Logger.SettleCount = SettleCount;
            m_Logger.ChangedCount = ChangedCount;

            m_Logger.MeanGenerateRuntimes = GeneratingRuntimes.Count == 0 ? 0 : (float)GeneratingRuntimes.Average();
            m_Logger.StdGenerateRuntimes = GeneratingRuntimes.Count == 0 ? 0 : (float)CalculateStandardDeviation(GeneratingRuntimes);

            m_Logger.MeanGenerateScores = GeneratingScores.Count == 0 ? 0 : (float)GeneratingScores.Average();
            m_Logger.StdGenerateScores = GeneratingScores.Count == 0 ? 0 : (float)CalculateStandardDeviation(GeneratingScores);

            m_Logger.KnowledgeReachStep = KnowledgeReachStep;
            m_Logger.KnowledgeQ1ReachStep = KnowledgeQ1ReachStep;
            m_Logger.KnowledgeQ2ReachStep = KnowledgeQ2ReachStep;
            m_Logger.KnowledgeQ3ReachStep = KnowledgeQ3ReachStep;

            FlushLog(GetMatchResultLogPath(), m_Logger);
        }

        private double CalculateStandardDeviation(List<float> numbers) {
            double mean = numbers.Average();
            double sumOfSquaredDifferences = numbers.Select(num => Mathf.Pow(num - (float)mean, 2)).Sum();
            double variance = sumOfSquaredDifferences / (numbers.Count - 1);
            double stdDev = Mathf.Sqrt((float)variance);
            return stdDev;
        }
        private double CalculateStandardDeviation(List<int> numbers) {
            double mean = numbers.Average();
            double sumOfSquaredDifferences = numbers.Select(num => Mathf.Pow(num - (float)mean, 2)).Sum();
            double variance = sumOfSquaredDifferences / (numbers.Count - 1);
            double stdDev = Mathf.Sqrt((float)variance);
            return stdDev;
        }
        private string GetMatchResultLogPath()
        {
            return ParameterManagerSingleton.GetInstance().GetParam("logPath") +
            "MatchResult_" + ParameterManagerSingleton.GetInstance().GetParam("runId") + "_" + m_uuid + ".csv";
        }

        public void FlushLog(string filePath, PCGStepLog log)
        {
            if (!File.Exists(filePath))
            {
                // Print whether the file exists or not
                using (StreamWriter sw = File.CreateText(filePath))
                {
                    string output = "";
                    output += "EpisodeCount,StepCount,Time,InstanceUUID,SettleCount,ChangedCount,MeanGenerateRuntimes,StdGenerateRuntimes,MeanGenerateScores,StdGenerateScores,ReachedKnowledgeStep,Q1ReachedKnowledgeStep,Q2ReachedKnowledgeStep,Q3ReachedKnowledgeStep,";

                    foreach (PieceType pieceType in BoardPCGAgent.PieceLogOrder)
                    {
                        output += $"Matched_{pieceType},";
                    }

                    foreach (PieceType pieceType in BoardPCGAgent.PieceLogOrder)
                    {
                        output += $"Target_{pieceType},";
                    }
                    output = output.Substring(0, output.Length - 1);

                    sw.WriteLine(output);
                }
            }

            // Append a file to write to csv file
            using (StreamWriter sw = File.AppendText(filePath))
            {
                sw.WriteLine(log.ToCSVRoW());
            }
        }


    }

    public class PCGStepLog
    {
        public int EpisodeCount;
        public int StepCount;
        public string Time;
        public string InstanceUUID;
        public SkillKnowledge SkillKnowledge;
        public int SettleCount;
        public int ChangedCount;
        public float MeanGenerateRuntimes;
        public float StdGenerateRuntimes;
        public float MeanGenerateScores;
        public float StdGenerateScores;
        public int KnowledgeReachStep;
        public int KnowledgeQ1ReachStep;
        public int KnowledgeQ2ReachStep;
        public int KnowledgeQ3ReachStep;

        public PCGStepLog()
        {

        }

        public string ToCSVRoW()
        {
            string row = "";
            row += EpisodeCount + ",";
            row += StepCount + ",";
            row += Time + ",";
            row += InstanceUUID + ",";
            row += SettleCount + ",";
            row += ChangedCount + ",";

            row += MeanGenerateRuntimes + ",";
            row += StdGenerateRuntimes + ",";

            row += MeanGenerateScores + ",";
            row += StdGenerateScores + ",";

            row += KnowledgeReachStep + ",";
            row += KnowledgeQ1ReachStep + ",";
            row += KnowledgeQ2ReachStep + ",";
            row += KnowledgeQ3ReachStep + ",";

            foreach (Dictionary<PieceType, int> table in new Dictionary<PieceType, int>[2] { SkillKnowledge.CurrentMatchCounts, SkillKnowledge.TargetMatchCounts })
            {
                foreach (PieceType pieceType in BoardPCGAgent.PieceLogOrder)
                {
                    row += table[pieceType] + ",";
                }
            }

            // Remove the last comma
            row = row.Substring(0, row.Length - 1);

            return row;
        }
    }

    public enum GeneratorType
    {
        Random = 0,
        MCTS = 1,
        Sampling = 2,
        GA = 3,
    }

    public enum AgentType
    {
        Agent = 0,
        Human = 1,
    }

    public enum GeneratorReward
    {
        Score = 0,
        WeighteScore = 3,
        Knowledge = 1,
        KnowledgePercentile = 2,
    }


}
