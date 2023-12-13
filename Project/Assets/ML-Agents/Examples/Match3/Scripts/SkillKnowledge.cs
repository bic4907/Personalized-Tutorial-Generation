using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.MLAgentsExamples
{

    public class SkillKnowledge
    {
        public Dictionary<PieceType, int> CurrentMatchCounts;
        public Dictionary<PieceType, int> TargetMatchCounts;
        public Dictionary<PieceType, bool> ManualCheck;
        public Dictionary<PieceType, int> SeenMatches;
        public Dictionary<PieceType, int> SeenDestroys;

        
        public PieceType[] PieceTypes = new PieceType[] {PieceType.HorizontalPiece, PieceType.VerticalPiece, PieceType.CrossPiece, PieceType.BombPiece, PieceType.RocketPiece, PieceType.RainbowPiece};

        public int DefaultTargetValue = 5;
        // Start is called before the first frame update
        public SkillKnowledge()
        {
            CurrentMatchCounts = new Dictionary<PieceType, int>();
            TargetMatchCounts = new Dictionary<PieceType, int>();
            ManualCheck = new Dictionary<PieceType, bool>();
            SeenMatches = new Dictionary<PieceType, int>();
            SeenDestroys = new Dictionary<PieceType, int>();

            for (int i = 0; i < PieceTypes.Length; i++)
            {
                CurrentMatchCounts.Add(PieceTypes[i], 0);
                TargetMatchCounts.Add(PieceTypes[i], DefaultTargetValue);
                ManualCheck.Add(PieceTypes[i], false);
                SeenMatches.Add(PieceTypes[i], 0);
                SeenDestroys.Add(PieceTypes[i], 0);
            }
            SeenMatches.Add(PieceType.NormalPiece, 0);
            SeenDestroys.Add(PieceType.NormalPiece, 0);
        }
        
        public SkillKnowledge(int HorizontalPieceCount, 
                            int VerticalPieceCount,
                            int CrossPieceCount,
                            int BombPieceCount,
                            int RocketPieceCount,
                            int RainbowPieceCount) : this()
        {
            
            TargetMatchCounts[PieceType.HorizontalPiece] = HorizontalPieceCount;
            TargetMatchCounts[PieceType.VerticalPiece] = VerticalPieceCount;
            TargetMatchCounts[PieceType.CrossPiece] = CrossPieceCount;
            TargetMatchCounts[PieceType.BombPiece] = BombPieceCount;
            TargetMatchCounts[PieceType.RocketPiece] = RocketPieceCount;
            TargetMatchCounts[PieceType.RainbowPiece] = RainbowPieceCount;
        }

        public void Reset()
        {
            foreach (PieceType pieceType in PieceTypes)
            {
                CurrentMatchCounts[pieceType] = 0;
                TargetMatchCounts[pieceType] = DefaultTargetValue;
            }
        }

        public Dictionary<PieceType, int> GetMatchCounts()
        {
            return CurrentMatchCounts;
        }

        public void IncreaseMatchCount(PieceType type)
        {
            CurrentMatchCounts[type]++;
        }

        public void IncreaseMatchCount(PieceType type, int count)
        {
            CurrentMatchCounts[type] += count;
        }

        public void IncreaseSeenMatches(PieceType type, int count)
        {
            SeenMatches[type] += count;
        }

        public void IncreaseSeenDestroys(PieceType type, int count)
        {
            SeenDestroys[type] += count;
        }

        public bool IsMatchCountReachedTarget(PieceType pieceType)
        {
            return CurrentMatchCounts[pieceType] >= TargetMatchCounts[pieceType];
        }

        public bool IsMatchCountAlmostReachedTarget(PieceType pieceType, float ratio)
        {
            if (TargetMatchCounts[pieceType] == 0)
            {
                return true;
            }
            else
            {
                return CurrentMatchCounts[pieceType] >= (int)Math.Ceiling(TargetMatchCounts[pieceType] * ratio);
            }
            
        }
        
        public bool IsAllBlockReachTarget()
        {
            foreach (PieceType pieceType in PieceTypes)
            {
                if (!IsMatchCountReachedTarget(pieceType))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsAllBlockAlmostReachTarget(float ratio)
        {
            foreach (PieceType pieceType in PieceTypes)
            {
                if (!IsMatchCountAlmostReachedTarget(pieceType, ratio))
                {
                    return false;
                }
            }

            return true;
        }

        public float GetMatchPercentile(PieceType pieceType)
        {
            float percentile;
            if (TargetMatchCounts[pieceType] == 0)
            {
                percentile = 1.0f;
            }
            else
            {
                percentile = (float)CurrentMatchCounts[pieceType] / (float)TargetMatchCounts[pieceType];
            }
            return Math.Max(Math.Min(percentile, 1.0f), 0.0f);
        }
        
        public SkillKnowledge DeepCopy()
        {
            SkillKnowledge result = new SkillKnowledge();

            // Clone dictionaries
            foreach (KeyValuePair<PieceType, int> entry in CurrentMatchCounts)
            {
                result.CurrentMatchCounts[entry.Key] = entry.Value;
            }
            foreach (KeyValuePair<PieceType, int> entry in TargetMatchCounts)
            {
                result.TargetMatchCounts[entry.Key] = entry.Value;
            }

            return result;
        }

        public override string ToString()
        {
            string result = "(SkillKnowledge) ";
            foreach (PieceType pieceType in PieceTypes)
            {
                result += pieceType.ToString() + ": " + CurrentMatchCounts[pieceType] + "/" + TargetMatchCounts[pieceType] + "/"  + ManualCheck[pieceType] + " | ";
            }
            result = result.Remove(result.Trim().Length - 2);
            
            return result;
        }

    }

    public class SkillKnowledgeExperimentSingleton
    {

        private static SkillKnowledgeExperimentSingleton m_Instance;

        public static SkillKnowledgeExperimentSingleton Instance { 
            get
            { 
                if (m_Instance == null) 
                {   
                    m_Instance = new SkillKnowledgeExperimentSingleton();
                }
                return m_Instance;
            } 
            set { value = m_Instance; }
        }

        private List<SkillKnowledge> SkillKnowledges;

        // Start is called before the first frame update
        public SkillKnowledgeExperimentSingleton()
        {
            SkillKnowledges = new List<SkillKnowledge>();
            /*
            player,index,time,event,matched_skill_0,learned_skill_0,matched_skill_1,learned_skill_1,matched_skill_2,learned_skill_2,matched_skill_3,learned_skill_3,matched_skill_4,learned_skill_4,matched_skill_5,learned_skill_5
            5,48,2022-11-08 13:16:42,GameAction,7,1,3,1,0,0,2,1,2,1,3,1
            6,108,2022-11-08 15:20:16,GameAction,11,1,8,1,4,1,3,0,5,1,3,1
            10,97,2022-11-08 19:32:28,GameAction,8,0,4,1,1,1,4,0,3,1,2,1
            11,91,2022-11-11 12:01:32,GameAction,7,1,8,1,2,1,3,1,17,1,4,1
            12,81,2023-01-02 19:09:06,GameAction,6,1,10,1,1,1,2,1,3,1,4,1
            13,98,2023-01-03 15:22:38,GameAction,4,1,3,1,2,1,2,0,6,1,2,0
            14,126,2023-01-03 17:23:28,GameAction,10,1,5,1,1,0,2,0,12,0,4,1
            15,88,2023-01-04 12:46:32,GameAction,9,1,3,1,2,1,1,1,14,1,4,1
            16,76,2023-01-03 19:07:14,GameAction,11,1,7,1,1,1,1,1,8,1,3,1
            17,112,2023-01-04 13:16:17,GameAction,10,1,8,1,7,1,4,1,9,1,5,1
            18,51,2023-01-04 13:28:49,GameAction,4,1,9,1,1,1,2,1,3,1,2,1
            */


            SkillKnowledges.Add(new SkillKnowledge(7, 3, 0, 2, 2, 3));
            SkillKnowledges.Add(new SkillKnowledge(11, 8, 4, 3, 5, 3));
            SkillKnowledges.Add(new SkillKnowledge(8, 4, 1, 4, 3, 2));
            SkillKnowledges.Add(new SkillKnowledge(7, 8, 2, 3, 17, 4));
            SkillKnowledges.Add(new SkillKnowledge(6, 10, 1, 2, 3, 4));
            SkillKnowledges.Add(new SkillKnowledge(4, 3, 2, 2, 6, 2));
            SkillKnowledges.Add(new SkillKnowledge(10, 5, 1, 2, 12, 4));
            SkillKnowledges.Add(new SkillKnowledge(9, 3, 2, 1, 14, 4));
            SkillKnowledges.Add(new SkillKnowledge(11, 7, 1, 1, 8, 3));
            SkillKnowledges.Add(new SkillKnowledge(10, 8, 7, 4, 9, 5));
            SkillKnowledges.Add(new SkillKnowledge(4, 9, 1, 2, 3, 2));
            
            SkillKnowledges.Add(new SkillKnowledge(2, 2, 2, 2, 2, 2)); // Dummy
        }

        public SkillKnowledge GetSkillKnowledge(int index)
        {
            return SkillKnowledges[index].DeepCopy();
        }

    }


}