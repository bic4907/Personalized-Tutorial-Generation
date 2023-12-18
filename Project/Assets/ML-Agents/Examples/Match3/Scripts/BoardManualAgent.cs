using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.MLAgents.Integrations.Match3;
using TMPro;

namespace Unity.MLAgentsExamples
{


    public class BoardManualAgent : MonoBehaviour
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

        public int PlayerNumber = 11;

        public int MCTS_Simulation = 300;

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
        public int KnowledgeAlmostReachStep = -1; // 3/4 percentqage of the target

        public List<int> ComparisonCounts;
        public bool SaveFirebaseLog = false;

        private FirebaseLogger m_FirebaseLogger;

        [Header("")]
        public AgentType agentType = AgentType.Agent;
        public MouseInteraction m_mouseInput;

        public float m_WaitingStartedTime;
        int m_CntChainEffect = 0;
        public float LastDecisionTime = Int16.MaxValue;

        public float SelfMatchingThreshold = 5.0f;
        public float HintStartTime = 5.0f; // Hint will be shown after this time
        public bool m_HintGlowed = false;
        public bool m_GlowOnlySpecialBlock = false;

        private Move LastHintMove;
        private Move LastPlayerMove;

        private TextMeshProUGUI m_LearningProgressText;
        private TextMeshProUGUI m_QuizProgressText;
        public List<Quiz> m_QuizList;
        public List<Quiz> m_SolvedQuizList;
        public Quiz m_CurrentQuiz;
        public int LearningStepCount = 0;
        BoardPresetManager m_presetManager;
        QuizUIManager m_QuizUIManager;
        TutoringUIManager m_TutoringUIManager;

        public ExperimentMode m_ExperimentMode = ExperimentMode.Learning;

        // MessageBox
        public GameObject PreExperimentMessageBox;
        public GameObject MidExperimentMessageBox;
        public GameObject PostExperimentMessageBox;

        public List<string> m_InitialBoardList;

        public List<string> m_KnowledgeEventList;

        private int LastPCGTime = 0;

        // Demo
        private TextMeshProUGUI m_DemoText;
        public bool IsDemo = false;
    

        private string GetMethodPostfix()
        {
            // Concat the generatorType and the reward
            string _postfix = "";

            switch (generatorType)
            {
                case GeneratorType.MCTS:
                    _postfix += "MC";
                    // Add the reward type
                    
                    switch(generatorRewardType)
                    {
                        case GeneratorReward.Score:
                            _postfix += "SC";
                            break;
                        case GeneratorReward.Knowledge:
                            _postfix += "KN";
                            break;
                    }

                    break;
                case GeneratorType.Random:
                    _postfix += "RD";
                    break;
            }
            
            if (m_GlowOnlySpecialBlock) 
            {
                _postfix += "HT";
            }

            return _postfix;
        }

        protected void Awake()
        {
            Board = GetComponent<Match3Board>();
            m_ModelOverrider = GetComponent<ModelOverrider>();
            m_Logger = new PCGStepLog();
            m_presetManager = GetComponent<BoardPresetManager>();
            m_KnowledgeEventList = new List<string>();

            if (!IsDemo) 
            {
                SetRandomGenerationMethod();
            }
            if (SaveFirebaseLog)
            {
                // Add FirebaseLogger component in this game objct
                if (this.gameObject.GetComponent<FirebaseLogger>() == null)
                {
                    m_FirebaseLogger = this.gameObject.AddComponent<FirebaseLogger>();
                    m_FirebaseLogger.SetUUID(this.GetMethodPostfix() + "_" + m_uuid);
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
                    case "kp":  // knowledge percentile
                        generatorRewardType = GeneratorReward.KnowledgePercentile;
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

            m_SkillKnowledge = SkillKnowledgeExperimentSingleton.Instance.GetSkillKnowledge(PlayerNumber);

            ComparisonCounts = new List<int>();
            InitializeQuiz();
            InitializeUI();
            
            // Show the PreExperimentMsg game object
            if (PreExperimentMessageBox)
            {
                PreExperimentMessageBox.SetActive(true);
            }

            m_InitialBoardList = new List<string>();            
            m_InitialBoardList.Add("init_0");
            m_InitialBoardList.Add("init_1");
            m_InitialBoardList.Add("init_2");
            m_InitialBoardList.Add("init_3");
            m_InitialBoardList.Add("init_4");


        }

        public void SetRandomGenerationMethod()
        {
            // Randomly select the generation method
            var _random = new System.Random(DateTime.Now.Millisecond);

            string[] _methods = new string[] { "mcts_knowledge", "mcts_score", "random" };
            // string[] _methods = new string[] { "mcts_knowledge" };

            // Sample one of method and write the case
            string _method = _methods[_random.Next() % _methods.Length];



            switch(_method)
            {
                case "mcts_knowledge":
                    generatorType = GeneratorType.MCTS;
                    generatorRewardType = GeneratorReward.Knowledge;
                    break;
                case "mcts_score":
                    generatorType = GeneratorType.MCTS;
                    generatorRewardType = GeneratorReward.Score;
                    break;
                case "random":
                    generatorType = GeneratorType.Random;
                    break;
            }

            Debug.Log($"Randomly selected generation method!\nGeneratorType: {generatorType}, GeneratorRewardType: {generatorRewardType}");
        }


        public void SetRandomInitialBoard()
        {
            if (m_InitialBoardList.Count > 0)
            {

                var _random = new System.Random(DateTime.Now.Millisecond);

                string _boardName = m_InitialBoardList[_random.Next() % m_InitialBoardList.Count];
                Debug.Log($"Loading initial board: {_boardName}");
                m_presetManager.LoadBoard(Board, _boardName);
            }
            //GlowTiles(GreedyMatch3Solver.GetAction(Board), isTwoWay: true);
        }

        private void InitializeQuiz()
        {
            m_QuizList = new List<Quiz>();
            m_SolvedQuizList = new List<Quiz>();


            m_QuizList.Add(new Quiz("horizon_0", PieceType.HorizontalPiece));
            m_QuizList.Add(new Quiz("horizon_1", PieceType.HorizontalPiece));

            m_QuizList.Add(new Quiz("vertical_0", PieceType.VerticalPiece));
            m_QuizList.Add(new Quiz("vertical_1", PieceType.VerticalPiece));

            m_QuizList.Add(new Quiz("cross_0", PieceType.CrossPiece));
            m_QuizList.Add(new Quiz("cross_1", PieceType.CrossPiece));

            m_QuizList.Add(new Quiz("bomb_0", PieceType.BombPiece));
            m_QuizList.Add(new Quiz("bomb_1", PieceType.BombPiece));

            m_QuizList.Add(new Quiz("rocket_0", PieceType.RocketPiece));
            m_QuizList.Add(new Quiz("rocket_1", PieceType.RocketPiece));

            m_QuizList.Add(new Quiz("rainbow_0", PieceType.RainbowPiece));
            m_QuizList.Add(new Quiz("rainbow_1", PieceType.RainbowPiece));

            // Shuffle m_QuizList
            m_QuizList = m_QuizList.OrderBy(x => Guid.NewGuid()).ToList();
        }

        private void InitializeUI()
        {
            if (GameObject.Find("LearningProgressTxt"))
            {
                m_LearningProgressText = GameObject.Find("LearningProgressTxt").GetComponent<TextMeshProUGUI>();
            }
            if (GameObject.Find("QuizProgressTxt"))
            {
                m_QuizProgressText = GameObject.Find("QuizProgressTxt").GetComponent<TextMeshProUGUI>();
            }
            if (GameObject.Find("QuizUIManager"))
            {
                m_QuizUIManager = GameObject.Find("QuizUIManager").GetComponent<QuizUIManager>();
                m_QuizUIManager.SetActive(false);
            }
            if (GameObject.Find("TutoringUIManager"))
            {
                m_TutoringUIManager = GameObject.Find("TutoringUIManager").GetComponent<TutoringUIManager>();
                m_TutoringUIManager.SetNumber(MaxMoves);
                m_TutoringUIManager.SetActive(true);
            }
            if (IsDemo && GameObject.Find("DemoText"))
            {
                m_DemoText = GameObject.Find("DemoText").GetComponent<TextMeshProUGUI>();
            }
        }

        public void OnEpisodeBegin()
        {
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

            Debug.Log($"Resetting Skill Knowledge: {m_SkillKnowledge}");

            CurrentEpisodeCount += 1;
            CurrentStepCount = 0;
            SettleCount = 0;
            ChangedCount = 0;
            LearningStepCount = 0;
            LastDecisionTime = Int16.MaxValue;

            m_WaitingStartedTime = Time.realtimeSinceStartup;
            m_CntChainEffect = 0;
            LastHintMove = new Move();
            LastPlayerMove = new Move();

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




        }

        void Update()
        {
            // if (Input.GetKeyDown(KeyCode.R))
            // {
            //     OnEpisodeBegin();
            // }

            if (m_LearningProgressText != null)
            {
                m_LearningProgressText.text = GetLearningProgressText();
            }
            if (m_QuizProgressText != null)
            {
                m_QuizProgressText.text = GetQuizProgressText();
            }

            if (LearningStepCount >= MaxMoves)
            {
                if (m_ExperimentMode == ExperimentMode.Learning)
                {
                    InitializeQuizMode();
                }
             
            }

            if (IsDemo) UpdateDemoText();

        }

        private void UpdateDemoText()
        {
            Debug.Log("UpdateDemoText");
            // print the generative method and the psm state
            string _text = "[Generator Type]\n";

            // if generatorType is MCTS, and the reward type is knowledge, print MCTS-PSM
            if (generatorType == GeneratorType.MCTS && generatorRewardType == GeneratorReward.Knowledge)
            {
                _text += "MCTS-PSM";
            }
            else if (generatorType == GeneratorType.MCTS && generatorRewardType == GeneratorReward.Score)
            {
                _text += "MCTS-GEN";
            }
            else if (generatorType == GeneratorType.Random)
            {
                _text += "Random";
            }

            if (generatorType == GeneratorType.MCTS && generatorRewardType == GeneratorReward.Knowledge)
            {
                // print m_SkillKnowledge
                _text += "\n\n";
                _text += "[Estimated Knowledge Status]\n";
                _text += m_SkillKnowledge.ToManualString();
            } else
            {
                _text += "\n\n";
                _text += "[Estimated Knowledge Status]\n";
                _text += "N/A";
            }

            _text += "\n\n";
            _text += "[Game Status]\n";
            _text += m_CurrentState.ToString();

            m_DemoText.text = _text;    

        }

        private string GetLearningProgressText()
        {
            return LearningStepCount + " / " + MaxMoves;
        }
        private string GetQuizProgressText()
        {
            return m_SolvedQuizList.Count + " / " + GetTotalQuizCount();
        }

        private int GetTotalQuizCount()
        {
            return m_QuizList.Count + m_SolvedQuizList.Count + (m_CurrentQuiz != null ? 1 : 0);
        }

        private void OnPlayerAction()
        {
            if (SaveFirebaseLog)
            {
                if (m_ExperimentMode == ExperimentMode.Learning)
                {
                    FirebaseLog.LearningLog log = new FirebaseLog.LearningLog();
                    log.EpisodeCount = CurrentEpisodeCount;
                    log.EpisodeStepCount = CurrentStepCount;
                    log.TotalStepCount = TotalStepCount;
                    log.DecisionTime = LastDecisionTime;
                    log.HintAction = LastHintMove.MoveIndex;
                    log.HintShown = m_HintGlowed;
                    log.PlayerAction = LastPlayerMove.MoveIndex;
                    log.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    log.InstanceUUID = m_uuid;
                    log.SkillKnowledge = m_SkillKnowledge;
                    log.Board = Board.m_Cells;
                    log.MatchEvent = m_KnowledgeEventList.ToArray();
                    log.LastPCGTime = LastPCGTime;
                    m_FirebaseLogger.Post(log);
                }
            }

        }

        private void InitializeQuizMode()
        {
            m_ExperimentMode = ExperimentMode.Quiz;  
            m_TutoringUIManager.SetActive(false);
            if (MidExperimentMessageBox)
            {
                MidExperimentMessageBox.SetActive(true);
            }
            NextQuiz();
        }

        private void NextQuiz()
        {
            if (m_CurrentQuiz != null)
            {
                m_SolvedQuizList.Add(m_CurrentQuiz);
            }
            m_CurrentQuiz = null;
            if (m_QuizList.Count > 0)
            {
                m_CurrentQuiz = m_QuizList[0];
                m_QuizList.RemoveAt(0);
            } else
            {
                if (m_FirebaseLogger)
                {
                    m_FirebaseLogger.PostDoneSignal();
                }
 
                Update();
                SetEmptyBoard();
            
            }

            if (m_CurrentQuiz != null)
            {
                
                m_CurrentState = State.WaitForMove;

                m_presetManager.LoadBoard(m_CurrentQuiz.FileName);
                m_QuizUIManager.SetNumber(m_SolvedQuizList.Count + 1);
                m_QuizUIManager.SetPieceType(m_CurrentQuiz.PieceType);
                m_QuizUIManager.SetActive(true);

                m_CurrentState = State.WaitForMove;
            } 
            else
            {
                m_QuizUIManager.SetActive(false);
            }
        }

        private void PostQuizLog(Quiz quiz)
        {

            FirebaseLog.QuizLog log = new FirebaseLog.QuizLog();
            log.QuestionNumber = m_SolvedQuizList.Count;
            log.QuizFile = quiz.FileName;
            log.PlayerAction = quiz.PlayerAction;
            log.DecisionTime = LastDecisionTime;
            log.Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            m_FirebaseLogger.Post(log);
 
        }

        private void SetEmptyBoard()
        {

            this.gameObject.SetActive(false);
            if (PostExperimentMessageBox)
            {
                PostExperimentMessageBox.SetActive(true);
                GameObject.Find("SurveyCodeInput").GetComponent<TMP_InputField>().text = this.GetMethodPostfix() + "_" + m_uuid;

            }
        }

        private void FixedUpdate()
        {
            // Make a move every step if we're training, or we're overriding models in CI.

            AnimatedUpdate();

            // We can't use the normal MaxSteps system to decide when to end an episode,
            // since different agents will make moves at different frequencies (depending on the number of
            // chained moves). So track a number of moves per Agent and manually interrupt the episode.

            MCTS.Instance.SetRewardMode(generatorRewardType);
            MCTS.Instance.SetSimulationLimit(MCTS_Simulation);
            MCTS.Instance.SetKnowledgeAlmostRatio(KnowledgeAlmostRatio);
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
                // AddReward(k_RewardMultiplier * pointsEarned);

                Board.SpawnSpecialCells();

                var createdPieces = SpecialMatch.GetMatchCount(Board.GetLastCreatedPiece());
                foreach (var (type, count) in createdPieces)
                {
                    m_SkillKnowledge.IncreaseMatchCount(type, count);
                    m_ManualSkillKnowledge.IncreaseMatchCount(type, count);
                }
                Board.ExecuteSpecialEffect();

                Board.DropCells();

                switch(generatorType)
                {
                    case GeneratorType.Random:
                        Board.FillFromAbove();
                        break;
                    case GeneratorType.MCTS:
                        MCTS.Instance.FillEmpty(Board, m_SkillKnowledge);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                CurrentStepCount += 1;
                TotalStepCount += 1;
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
            Move move = GreedyMatch3Solver.GetAction(Board);
            Board.MakeMove(move);
            OnPlayerAction();

            m_MovesMade++;
        }

        void AnimatedUpdate()
        {
            m_TimeUntilMove -= Time.deltaTime;
            if (m_TimeUntilMove > 0.0f)
            {
                return;
            }

            m_TimeUntilMove = MoveTime;

            State nextState;

            // Check for the first findMatches after the move action.
            switch (m_CurrentState)
            {
                case State.FindMatches:
                    // Execute only State is not Quiz Mode
                    if (Board.GetEmptyCellCount() > 0)
                    {
                        nextState = State.ClearMatched;
                        
                    }
                    else
                    {
       
                        var hasMatched = Board.MarkMatchedCells();
                        nextState = hasMatched ? State.ClearMatched : State.WaitForMove;
                        if (nextState == State.WaitForMove)
                        {
                            m_WaitingStartedTime = Time.realtimeSinceStartup;
                            m_MovesMade++;
                        }
        
                    }

                    break;
       
                case State.ClearMatched:
                    m_CntChainEffect++;

                    var pointsEarned = Board.ClearMatchedCells();
                    // AddReward(k_RewardMultiplier * pointsEarned);

                    Board.SpawnSpecialCells();

                    var createdPieces = Board.GetLastCreatedPiece();

                    if (m_CntChainEffect == 1)
                    {


                        foreach (var (type, count) in SpecialMatch.GetMatchCount(createdPieces))
                        {
                            
                            m_SkillKnowledge.IncreaseMatchCount(type, count);

                            if (m_SkillKnowledge.ManualCheck[type] == true || count == 0) continue;

                            if (LastDecisionTime < SelfMatchingThreshold)
                            {
                                // Before hint
                                Debug.Log("LastDecisionTime < SelfMatchingThreshold: " + type);
                                m_KnowledgeEventList.Add("MatchNoHint");
                                m_SkillKnowledge.ManualCheck[type] = true;
                            }
                            else
                            {
                                // After hint
                                if (LastHintMove.MoveIndex != LastPlayerMove.MoveIndex)
                                {
                                    Debug.Log("LastDecisionTime >= SelfMatchingThreshold, but Ignored Hint: " + type);
                                    m_KnowledgeEventList.Add("MatchIgnoringHint");
                                    m_SkillKnowledge.ManualCheck[type] = true;

                                }
                            }


                        }
                        Debug.Log(m_SkillKnowledge);
                    }

                    Board.ExecuteSpecialEffect();



                    nextState = State.Drop;
                    break;
                case State.Drop:
                    Board.DropCells();
                    nextState = State.FillEmpty;
                    break;
                case State.FillEmpty:

                    var startTime = Time.realtimeSinceStartup;

                    switch(generatorType)
                    {
                        case GeneratorType.Random:
                            Board.FillFromAbove();
                            break;
                        case GeneratorType.MCTS:
                            MCTS.Instance.FillEmpty(Board, m_SkillKnowledge);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    float elapsedTimeSeconds = Time.realtimeSinceStartup - startTime;
                    LastPCGTime = (int)(elapsedTimeSeconds * 1000f);

                    nextState = State.FindMatches;
                    break;
                case State.WaitForMove:

                    bool isBoardSettled = false;
                    nextState = State.WaitForMove;

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


                    float WaitedTime = Time.realtimeSinceStartup - m_WaitingStartedTime;
                    if (WaitedTime > HintStartTime && m_HintGlowed == false && m_ExperimentMode == ExperimentMode.Learning)
                    {
                        Move greedyAction = GreedyMatch3Solver.GetAction(Board);
                        if (m_GlowOnlySpecialBlock == false)
                        {
                            // GlowTiles(greedyAction, isTwoWay: true);
                        }
                        else
                        {
                            PieceType matchablePiece = Board.IsMoveSpecialMatch(greedyAction);
                            if (matchablePiece != PieceType.NormalPiece && m_SkillKnowledge.ManualCheck[matchablePiece] == false)
                            {
                                // GlowTiles(greedyAction, isTwoWay: true);
                            }
                            else
                            {
                                Debug.Log("No Special Block Hint: " + greedyAction.MoveIndex);
                            }
                        }
                        m_HintGlowed = true;
                    }


                    Move move = new Move();
                    switch(agentType)
                    {
                        case AgentType.Human:
                            if(m_mouseInput.playerHadVaildAction == true)
                            {
                                LastHintMove = GreedyMatch3Solver.GetAction(Board);

                                move = m_mouseInput.GetMove();
                                LastPlayerMove = move;

                                CurrentStepCount += 1;
                                TotalStepCount += 1;

                                LastDecisionTime = Time.realtimeSinceStartup - m_WaitingStartedTime;


                                /* Log */

                                foreach (var (type, count) in SpecialMatch.GetMatchCount(Board.GetLastSeenCreatedPiece(), true))
                                {
                                    m_SkillKnowledge.IncreaseSeenMatches(type, count);
                                }
                                // Move Event Log
                                foreach (var (type, count) in SpecialMatch.GetMatchCount(Board.GetLastSeenDestroyedPiece(), true))
                                {
                                    if (type == PieceType.NormalPiece) continue;
                                    m_SkillKnowledge.IncreaseSeenDestroys(type, count);
                                }
                                Board.ClearLastSeenPieceLog();
                            
                                OnPlayerAction();

                                // Debug.Log(move.MoveIndex);
                                if (ExperimentMode.Quiz != m_ExperimentMode)
                                {
                                    bool isMatched = Board.MakeMove(move);
                                    if (isMatched)
                                    {
                                        Board.ExecuteSpecialEffect();
                                    }
                                }
                                m_KnowledgeEventList.Clear();



                                m_CntChainEffect = 0;
                                nextState = State.FindMatches;
                                m_HintGlowed = false; // Reset
                                StopGlowingTiles();
                        
                                if (m_ExperimentMode == ExperimentMode.Learning)
                                {
                                    LearningStepCount += 1;
                                }
                                else if (m_ExperimentMode == ExperimentMode.Quiz)
                                {
                                    m_CurrentQuiz.PlayerAction = move.MoveIndex;
                                    
                                    if (m_FirebaseLogger != null) 
                                    {
                                        PostQuizLog(m_CurrentQuiz);
                                    }
                                    NextQuiz();
                                }

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
            KnowledgeAlmostReachStep = -1;
        }


        public void CheckKnowledgeReach()
        {
            if (KnowledgeReachStep == -1 && m_SkillKnowledge.IsAllBlockReachTarget())
            {
                KnowledgeReachStep = CurrentStepCount;
            }
            if (KnowledgeAlmostReachStep == -1 && m_SkillKnowledge.IsAllBlockAlmostReachTarget(KnowledgeAlmostRatio))
            {
                KnowledgeAlmostReachStep = CurrentStepCount;
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

        public void GlowTiles(Move move, bool isTwoWay = false)
        {
            GetComponent<Match3Drawer>().GlowTiles(move, isTwoWay);
        }
        public void StopGlowingTiles()
        {
            GetComponent<Match3Drawer>().StopGlowingTiles();
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

            // m_Logger.MeanComparisonCount = ComparisonCounts.Count == 0 ? 0 : (float)ComparisonCounts.Average();
            // m_Logger.StdComparisonCount = ComparisonCounts.Count == 0 ? 0 : (float)CalculateStandardDeviation(ComparisonCounts);

            m_Logger.KnowledgeReachStep = KnowledgeReachStep;
            // m_Logger.KnowledgeAlmostReachStep = KnowledgeAlmostReachStep;

            FlushLog(GetMatchResultLogPath(), m_Logger);
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
                    output += "EpisodeCount,StepCount,Time,InstanceUUID,SettleCount,ChangedCount,MeanComparisonCount,StdComparisonCount,ReachedKnowledgeStep,AlmostReachedKnowledgeStep,";

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

        public void SetDecisionStartTimeToNow()
        {
            Debug.Log("SetDecisionStartTimeToNow");
            m_WaitingStartedTime = Time.realtimeSinceStartup;
            GetComponent<Match3Drawer>().StopGlowingTiles();
        }



    }

    public enum ExperimentMode
    {
        Learning = 1,
        Quiz = 2
    }

    public class Quiz
    {
        public string FileName;
        public PieceType PieceType;
        public int PlayerAction;

        public Quiz(string fileName, PieceType pieceType)
        {
            FileName = fileName;
            PieceType = pieceType;
            PlayerAction = -1;
        }

        public bool IsSolved()
        {
            return PlayerAction != -1;
        }
    }
}
