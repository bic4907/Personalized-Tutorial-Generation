using Unity.MLAgents.Integrations.Match3;

namespace Unity.MLAgentsExamples
{
    public class Match3ExampleActuator : Match3Actuator
    {
        private Match3Board m_Board;
        Match3Board Board => m_Board;

        public Match3ExampleActuator(Match3Board board,
            bool forceHeuristic,
            string name,
            int seed
        )
            : base(board, forceHeuristic, seed, name)
        {
            m_Board = board;
        }


        protected override int EvalMovePoints(Move move)
        {
            return m_Board.EvalMovePoints(move);
        }
            
    }

}
