using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Integrations.Match3;

namespace Unity.MLAgentsExamples
{
    public class GreedyMatch3Solver : MonoBehaviour
    {

        // Start is called before the first frame update
        void Awake()
        {

        }

        // Update is called once per frame
        void Update()
        {
            
        }
     
        public static Move GetAction(Match3Board board)
        {

            int maxPoints = -999999;
            Move optimalMove = new Move();

            foreach (var move in board.ValidMoves())
            {
                int points = board.EvalMovePoints(move);

                if(maxPoints <= points)
                {
                    optimalMove = move;
                    maxPoints = points;
                }
            }
            
            return optimalMove;
        }


    }
}
