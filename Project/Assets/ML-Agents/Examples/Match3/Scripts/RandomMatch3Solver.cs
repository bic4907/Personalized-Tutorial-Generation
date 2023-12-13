using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Integrations.Match3;

namespace Unity.MLAgentsExamples
{
    public class RandomMatch3Solver : MonoBehaviour
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
            List<Move> validMovesList = new List<Move>(board.ValidMoves());
            // Sample random moves from validMovesList
            Move optimalMove = validMovesList[Random.Range(0, validMovesList.Count)];

            return optimalMove;
        }


    }
}
