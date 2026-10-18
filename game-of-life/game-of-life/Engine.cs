using System;
using System.Collections.Generic;
using System.Text;

namespace game_of_life
{
    //internal class Engine
    public sealed class Engine
    {
        private readonly bool[,] cells;

        public int Width => cells.GetLength(1);
        public int Height => cells.GetLength(0);

        public bool[,] Cells => cells;

        public Engine(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            cells = new bool[height, width];
        }

        public bool IsAlive(int row, int col)
        {
            ValidateCoordinates(row, col);
            return cells[row, col];
        }

        public bool ToggleCell(int row, int col)
        {
            ValidateCoordinates(row, col);

            cells[row, col] = !cells[row, col];
            return cells[row, col];
        }

        private void ValidateCoordinates(int row, int col)
        {
            if (row < 0 || row >= Height || col < 0 || col >= Width)
            {
                throw new ArgumentOutOfRangeException(nameof(row), "Cell coordinates are outside the grid.");
            }
        }
    }
}