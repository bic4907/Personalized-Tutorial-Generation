using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unity.MLAgentsExamples
{
    public class SpecialEffect
    {
        public SpecialEffect() {  }
        public SpecialEffect(int column, int row, PieceType specialType, int cellType)
        {
            Column = column;
            Row = row;
            SpecialType = specialType;
            CellType = cellType;
        }

        // Positional
        public int Column;
        public int Row;

        public PieceType SpecialType;

        public int CellType;

    }
}