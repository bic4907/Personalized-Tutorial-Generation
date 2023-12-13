using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Unity.MLAgents.Integrations.Match3;
using Unity.MLAgentsExamples;

//namespace Unity.MLAgentsExamples

public class GameSnapshotShooter : MonoBehaviour
{
    // Start is called before the first frame update
    
    public List<DemostrationSample> Random_Demostrations;
    public List<DemostrationSample> NOPSM_Demostrations;
    public List<DemostrationSample> PSM_Demostrations;
    public List<DemostrationSample> CurrentDemostrations;

    public int NumSamples = 10;
    public int CurrentIndex = 0;

    public DemostrationSample sample;

    public DemostrationState m_DemostrationState;
    public DemostrationState m_DemostrationCameraState;
    public DemostrationState m_DemostrationNextState;
    public DemostrationMethod m_DemostrationMethod;
    public SkillKnowledge m_SkillKnowledge;

    public int m_GenerateTryCount = 0;

    public Match3Board Board;
    public string rootPath;

    void Start()
    {
        Board = GetComponent<Match3Board>();

        // Get StreamingAsset directory
        rootPath = Path.Combine("D:\\", "Output", "Quantitative");
        // Make directory if not exists
        if (!Directory.Exists(rootPath))
        {
            Directory.CreateDirectory(rootPath);
        }

        Random_Demostrations = new List<DemostrationSample>();
        NOPSM_Demostrations = new List<DemostrationSample>();
        PSM_Demostrations = new List<DemostrationSample>();

        CurrentDemostrations = Random_Demostrations;

        m_DemostrationState = DemostrationState.StepA;
        m_DemostrationMethod = DemostrationMethod.Random;

        CurrentIndex = 0;

    }
    
    void Update()
    {
        AnimatedUpdate();
    }

    void AnimatedUpdate()
    {

        switch(m_DemostrationState)
        {
            case DemostrationState.Screenshot:
                CaptureScreenshot(m_DemostrationCameraState.ToString() + ".png");
                Debug.Log(m_DemostrationCameraState.ToString() + ".png");
                m_DemostrationState = m_DemostrationNextState;
                break;
            case DemostrationState.StepA:

                m_GenerateTryCount = 0;

                sample = new DemostrationSample();
                
                sample.Id = m_DemostrationMethod.ToString() + "_" + CurrentDemostrations.Count.ToString();
                sample.Generator = m_DemostrationMethod.ToString();
                sample.Skip = false;

                m_SkillKnowledge = new SkillKnowledge(1, 1, 1, 1, 1, 1);

                Board.ClearMarked();
                Board.ClearCreatedCell();
                if (CurrentDemostrations == Random_Demostrations)
                {
                    Board.InitSettled();
            
                    bool IsValidBoard = true;
                    var matchables = Board.GetSpecialMatchableChance();
                    foreach (PieceType pieceType in m_SkillKnowledge.PieceTypes)
                    {
                        if(matchables[pieceType] > 0)
                        {
                            IsValidBoard = false;
                            break;
                        }
                    }

                    if (IsValidBoard == false)
                    {
                        Debug.Log("Invalid Board, resetting...");
                        break;
                    }

                    m_SkillKnowledge = new SkillKnowledge(1, 1, 1, 1, 1, 1);
                    sample.SkillKnowledgeA = m_SkillKnowledge.DeepCopy();
  
                    // Random sample 0 or 1
                    foreach (PieceType pieceType in m_SkillKnowledge.PieceTypes)
                    {
                        m_SkillKnowledge.CurrentMatchCounts[pieceType] = Random.Range(0, 2);
                    }



                }
                else
                {
                    m_SkillKnowledge = Random_Demostrations[CurrentDemostrations.Count].SkillKnowledgeA.DeepCopy();
                    Board.m_Cells = Random_Demostrations[CurrentDemostrations.Count].BoardCellsA.Clone() as (int CellType, int SpecialType)[,];
                    
                }

                sample.SkillKnowledgeA = m_SkillKnowledge.DeepCopy();
                sample.BoardCellsA = ((int CellType, int SpecialType)[,])Board.m_Cells.Clone();
                // CaptureScreenshot("BoardCellsA.png");
                //Debug.Log("BoardCellsA");

                m_DemostrationState = DemostrationState.Screenshot;
                m_DemostrationCameraState = DemostrationState.StepA;
                m_DemostrationNextState = DemostrationState.StepB;
                break;

            case DemostrationState.StepB:
            // 여기서 상태 변경

                if (CurrentDemostrations == Random_Demostrations)
                {
                    Move move2 = GreedyMatch3Solver.GetAction(Board);

                    Board.MakeMove(move2);
                    Board.MarkMatchedCells();
                    Board.ClearMatchedCells();
                    Board.SpawnSpecialCells();
                    Board.ExecuteSpecialEffect();
                    Board.DropCells();

                    var hasMatched2 = Board.MarkMatchedCells();
                    if (hasMatched2 == true)
                    {
                        m_DemostrationState = DemostrationState.StepA;
                        Debug.Log("hasMatched found, resetting...");
                    }

                    sample.BoardCellsB = ((int CellType, int SpecialType)[,])Board.m_Cells.Clone();
                    sample.BoardCellsBMatchables = Board.GetSpecialMatchableChance();


                }
                else
                {     
                    Board.m_Cells = Random_Demostrations[CurrentDemostrations.Count].BoardCellsB.Clone() as (int CellType, int SpecialType)[,];
                    sample.BoardCellsBMatchables = Random_Demostrations[CurrentDemostrations.Count].BoardCellsBMatchables;
                }

                sample.BoardCellsBEmpty = Board.GetEmptyCellCount();

                m_DemostrationState = DemostrationState.Screenshot;
                m_DemostrationCameraState = DemostrationState.StepB;
                m_DemostrationNextState = DemostrationState.StepC;
                break;

            case DemostrationState.StepC:

                var m_Cells_tmp = Board.m_Cells.Clone();

                // Initial the Random generator with the millis
                int seed = System.DateTime.Now.Millisecond;
                // System.Random _random = new System.Random(seed);

                GeneratorType generatorType = GeneratorType.MCTS;
                GeneratorReward generatorRewardType = GeneratorReward.Score;

                switch(m_DemostrationMethod)
                {
                    case DemostrationMethod.PSM:
                        generatorType = GeneratorType.MCTS;
                        generatorRewardType = GeneratorReward.Knowledge;
                        MCTS.Instance.SetRewardMode(generatorRewardType);
                        break;
                    case DemostrationMethod.NOPSM:
                        generatorType = GeneratorType.MCTS;
                        generatorRewardType = GeneratorReward.Score;
                        MCTS.Instance.SetRewardMode(generatorRewardType);
                        break;
                    case DemostrationMethod.Random:
                        generatorType = GeneratorType.Random;
                        break;
                }
                
                
                switch(generatorType)
                {
                    case GeneratorType.Random:
                        Board.FillFromAbove();
                        break;
                    case GeneratorType.MCTS:
                        MCTS.Instance.FillEmpty(Board, m_SkillKnowledge);
                        break;
                }

                var hasMatched = Board.MarkMatchedCells();
                if (hasMatched == true)
                {

                    
                    m_GenerateTryCount++;

                    if (m_GenerateTryCount >= 5)
                    {
                        sample.Skip = true;

                        if (CurrentDemostrations != Random_Demostrations)
                        {
                            CurrentDemostrations.Add(sample);
                        }
                        m_DemostrationState = DemostrationState.StepA;
                        Debug.Log("m_GenerateTryCount > threshold, resetting...");
                    }
 

                    break;
                }


                sample.BoardCellsC = ((int CellType, int SpecialType)[,])Board.m_Cells.Clone();

                sample.BoardCellsCMatchables = Board.GetSpecialMatchableChance();


                m_DemostrationState = DemostrationState.Screenshot;
                m_DemostrationCameraState = DemostrationState.StepC;
                m_DemostrationNextState = DemostrationState.StepD;
                break;

            case DemostrationState.StepD:

                Move move = GreedyMatch3Solver.GetAction(Board);
                Board.MakeMove(move);

                sample.BoardCellsD = ((int CellType, int SpecialType)[,])Board.m_Cells.Clone();
                //CaptureScreenshot("BoardCellsD.png");
                //Debug.Log("BoardCellsD");

                m_DemostrationState = DemostrationState.Screenshot;
                m_DemostrationCameraState = DemostrationState.StepD;
                m_DemostrationNextState = DemostrationState.StepE;
                break;            

            case DemostrationState.StepE:

                Board.MarkMatchedCells();
                Board.ClearMatchedCells();
                Board.SpawnSpecialCells();

                var createdPieces = SpecialMatch.GetMatchCount(Board.GetLastCreatedPiece());
                foreach (var (type, count) in createdPieces)
                {
                    m_SkillKnowledge.IncreaseMatchCount(type, count);
                }
                sample.SkillKnowledgeE = m_SkillKnowledge.DeepCopy();


                sample.BoardCellsE = ((int CellType, int SpecialType)[,])Board.m_Cells.Clone();
                //CaptureScreenshot("BoardCellsE.png");
                //Debug.Log("BoardCellsE");

                CurrentDemostrations.Add(sample);

                // Switch
                if (CurrentDemostrations.Count >= NumSamples)
                {
                    if (CurrentDemostrations == Random_Demostrations)
                    {
                        CurrentDemostrations = NOPSM_Demostrations;
                        m_DemostrationMethod = DemostrationMethod.NOPSM;
                    }
                    else if (CurrentDemostrations == NOPSM_Demostrations)
                    {
                        CurrentDemostrations = PSM_Demostrations;
                        m_DemostrationMethod = DemostrationMethod.PSM;

                    }
                    else if (CurrentDemostrations == PSM_Demostrations)
                    {
                        // Quit Application also Editor
                        UnityEditor.EditorApplication.isPlaying = false;
                    }
                    CurrentIndex = 0;
                }
                else
                {
                    CurrentIndex += 1;
                }

                Dictionary<string, object> dict = sample.ToDict();

                string filePath = Path.Combine(rootPath, sample.Id, "result.json");
                File.WriteAllText(filePath, JObject.FromObject(dict).ToString());

                m_DemostrationState = DemostrationState.Screenshot;
                m_DemostrationCameraState = DemostrationState.StepE;
                m_DemostrationNextState = DemostrationState.StepA;
                break;            
        }
    }
    
    void CaptureScreenshot(string fileName)
    {
        
        string wsPath = Path.Combine(rootPath, sample.Id);
        if (!Directory.Exists(wsPath))
        {
            Directory.CreateDirectory(wsPath);
        }

        string filePath = Path.Combine(rootPath, sample.Id, fileName);
        ScreenCapture.CaptureScreenshot(filePath);
    }

    void SaveJson()
    {
        string wsPath = Path.Combine(rootPath, sample.Id);
        if (!Directory.Exists(wsPath))
        {
            Directory.CreateDirectory(wsPath);
        }

        string filePath = Path.Combine(rootPath, sample.Id, "result.json");
        File.WriteAllText(filePath, JObject.FromObject(sample).ToString());
    }



}

public class DemostrationSample
{
    public string Id;
    public string Generator;
    public (int CellType, int SpecialType)[,] BoardCellsA; // 빈칸을 만들기 위한 랜덤 보드
    public Move PlayerAction1; // 빈칸을 만들기 위한 랜덤 액션
    public SkillKnowledge SkillKnowledgeA;
    public (int CellType, int SpecialType)[,] BoardCellsB; // 빈칸을 만든 후의 보드 
    public (int CellType, int SpecialType)[,] BoardCellsC; // Generated 된 후의 보드 (여기 뭐 매칭가능한지 체크해야함)
    public Dictionary<PieceType, int> BoardCellsCMatchables = new Dictionary<PieceType, int>();
    public Dictionary<PieceType, int> BoardCellsBMatchables = new Dictionary<PieceType, int>();

    public Move PlayerAction2; // Generated 된 후의 플레이어의 액션
    public (int CellType, int SpecialType)[,] BoardCellsD; // Generated된 보드를 플레이어가 수행하고 블럭 교환만 된 것
    public (int CellType, int SpecialType)[,] BoardCellsE; // Generated된 보드를 플레이어가 수행하고 특수 블럭이 생겨난 것
    public SkillKnowledge SkillKnowledgeE;
    public int BoardCellsBEmpty;
    public bool Skip;

    public Dictionary<string, object> ToDict()
    {
        // Export the information exception for the BoardCells variables
        
        Dictionary<string, object> dict = new Dictionary<string, object>();

        foreach (PieceType pieceType in SkillKnowledgeA.PieceTypes)
        {
            dict["KN_A_" + pieceType.ToString()] = SkillKnowledgeA.CurrentMatchCounts[pieceType];
        }
        foreach (PieceType pieceType in SkillKnowledgeE.PieceTypes)
        {
            dict["KN_E_" + pieceType.ToString()] = SkillKnowledgeE.CurrentMatchCounts[pieceType];
        }
        foreach (PieceType pieceType in SkillKnowledgeE.PieceTypes)
        {
            dict["B_Matchable_" + pieceType.ToString()] = BoardCellsBMatchables[pieceType];
        }
        foreach (PieceType pieceType in SkillKnowledgeE.PieceTypes)
        {
            dict["C_Matchable_" + pieceType.ToString()] = BoardCellsCMatchables[pieceType];
        }

        dict["Generator"] = Generator;
        dict["BoardCellsBEmpty"] = BoardCellsBEmpty;
        dict["Skil"] = Skip;

        return dict;
        // To file


        
    }

}


public enum DemostrationState
{
    StepA,
    StepB,
    StepC,
    StepD,
    StepE,
    StepF,
    Screenshot,

}

public enum DemostrationMethod
{
    Random, NOPSM, PSM
}