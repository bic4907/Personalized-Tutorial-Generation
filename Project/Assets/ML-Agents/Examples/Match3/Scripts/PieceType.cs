using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.MLAgentsExamples
{

    public enum PieceType: int
    {
        None = -1,
        Empty = 0,
        NormalPiece = 1,
        HorizontalPiece = 2,
        VerticalPiece = 3,
        CrossPiece = 4,
        BombPiece = 5,
        RocketPiece = 6,
        RainbowPiece = 7
    }

}