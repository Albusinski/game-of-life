using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace game_of_life
{
    //internal class BoardRenderer
    public sealed class BoardRenderer : FrameworkElement
    {
        private static readonly Color BackgroundColor = Color.FromRgb(0x18, 0x1A, 0x20);
        private static readonly Color AliveColor = Color.FromRgb(0x75, 0xD6, 0x9C);
        private static readonly Color GridColor = Color.FromRgb(0x37, 0x3C, 0x48);
        private static readonly Brush BackgroundBrush = CreateBrush(BackgroundColor);
        private static readonly Brush AliveBrush = CreateBrush(AliveColor);
        private static readonly Pen GridPen = CreatePen(GridColor);
        private bool[,]? cells;
        private readonly Dictionary<int, SortedSet<int>> aliveCells = new();
        public double Zoom { get; private set; } = 1.0;
        public double PanX { get; private set; }
        public double PanY { get; private set; }

        public BoardRenderer()
        {
            ClipToBounds = true;
            Focusable = false;
        }

        private static SolidColorBrush CreateBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static Pen CreatePen(Color color)
        {
            var pen = new Pen(CreateBrush(color), 1);
            pen.Freeze();
            return pen;
        }

        public void SetCells(bool[,] newCells)
        {
            cells = newCells ?? throw new ArgumentNullException(nameof(newCells));

            RebuildAliveIndex();
            InvalidateVisual();
        }

        public void SetCellState(int row, int col, bool alive)
        {
            if (cells == null || row < 0 || row >= cells.GetLength(0) || col < 0 || col >= cells.GetLength(1))
            {
                return;
            }

            if (alive)
            {
                if (!aliveCells.TryGetValue(row, out var columns))
                {
                    columns = new SortedSet<int>();
                    aliveCells[row] = columns;
                }

                columns.Add(col);
            }
            else if (aliveCells.TryGetValue(row, out var columns))
            {
                columns.Remove(col);

                if (columns.Count == 0)
                {
                    aliveCells.Remove(row);
                }
            }
            InvalidateVisual();
        }

        private void RebuildAliveIndex()
        {
            aliveCells.Clear();

            if (cells == null)
            {
                return;
            }

            int height = cells.GetLength(0);
            int width = cells.GetLength(1);

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    if (!cells[row, col])
                    {
                        continue;
                    }

                    if (!aliveCells.TryGetValue(row, out var columns))
                    {
                        columns = new SortedSet<int>();
                        aliveCells[row] = columns;
                    }

                    columns.Add(col);
                }
            }
        }

        public void ResetView()
        {
            Zoom = 1.0;
            PanX = 0;
            PanY = 0;

            InvalidateVisual();
        }

        public void PanBy(Vector delta)
        {
            PanX += delta.X;
            PanY += delta.Y;

            InvalidateVisual();
        }

        public void ZoomAt(Point position, double factor, double minZoom, double maxZoom)
        {
            if (cells == null || ActualWidth <= 0 || ActualHeight <= 0)
            {
                return;
            }

            double oldZoom = Zoom;
            double newZoom = Math.Clamp(oldZoom * factor, minZoom, maxZoom);

            if (newZoom == oldZoom)
            {
                return;
            }

            GetGeometry(out double baseCellSize, out double offsetX, out double offsetY);

            if (baseCellSize <= 0)
            {
                return;
            }

            double gridX = (position.X - PanX - offsetX * oldZoom) / (baseCellSize * oldZoom);

            double gridY = (position.Y - PanY - offsetY * oldZoom) / (baseCellSize * oldZoom);

            Zoom = newZoom;

            PanX = position.X - offsetX * Zoom - gridX * baseCellSize * Zoom;

            PanY = position.Y - offsetY * Zoom - gridY * baseCellSize * Zoom;

            InvalidateVisual();
        }

        public bool TryGetCellAt(Point position, out int row, out int col)
        {
            row = -1;
            col = -1;

            if (cells == null || ActualWidth <= 0 || ActualHeight <= 0)
            {
                return false;
            }

            GetGeometry(out double baseCellSize, out double offsetX, out double offsetY);

            double cellSize = baseCellSize * Zoom;

            if (cellSize <= 0)
            {
                return false;
            }

            offsetX = offsetX * Zoom + PanX;
            offsetY = offsetY * Zoom + PanY;

            col = (int)Math.Floor((position.X - offsetX) / cellSize);

            row = (int)Math.Floor((position.Y - offsetY) / cellSize);

            return row >= 0 && row < cells.GetLength(0) && col >= 0 && col < cells.GetLength(1);
        }

        private void GetGeometry(out double baseCellSize, out double offsetX, out double offsetY)
        {
            int width = cells!.GetLength(1);
            int height = cells.GetLength(0);

            baseCellSize = Math.Min(ActualWidth / width, ActualHeight / height);

            offsetX = (ActualWidth - width * baseCellSize) / 2;
            offsetY = (ActualHeight - height * baseCellSize) / 2;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double canvasWidth = ActualWidth;
            double canvasHeight = ActualHeight;

            dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, canvasWidth, canvasHeight));

            if (cells == null || canvasWidth <= 0 || canvasHeight <= 0)
            {
                return;
            }

            GetGeometry(out double baseCellSize, out double offsetX, out double offsetY);

            double cellSize = baseCellSize * Zoom;

            if (cellSize <= 0)
            {
                return;
            }

            offsetX = offsetX * Zoom + PanX;
            offsetY = offsetY * Zoom + PanY;

            int width = cells.GetLength(1);
            int height = cells.GetLength(0);

            int firstCol = Math.Clamp((int)Math.Floor(-offsetX / cellSize), 0, width);

            int lastCol = Math.Clamp((int)Math.Ceiling((canvasWidth - offsetX) / cellSize), 0, width);

            int firstRow = Math.Clamp((int)Math.Floor(-offsetY / cellSize), 0, height);

            int lastRow = Math.Clamp((int)Math.Ceiling((canvasHeight - offsetY) / cellSize), 0, height);

            if (lastCol <= firstCol || lastRow <= firstRow)
            {
                return;
            }

            if (cellSize < 1)
            {
                DrawMinifiedCells(dc, firstRow, lastRow, firstCol, lastCol, offsetX, offsetY, cellSize, canvasWidth, canvasHeight);
            }
            else
            {
                DrawCells(dc, firstRow, lastRow, firstCol, lastCol, offsetX, offsetY, cellSize);
            }

            if (cellSize >= 4)
            {
                DrawGrid(dc, firstRow, lastRow, firstCol, lastCol, offsetX, offsetY, cellSize, canvasWidth, canvasHeight);
            }
        }

        private void DrawCells(DrawingContext dc, int firstRow, int lastRow, int firstCol, int lastCol, double offsetX, double offsetY, double cellSize)
        {
            for (int row = firstRow; row < lastRow; row++)
            {
                if (!aliveCells.TryGetValue(row, out var columns))
                {
                    continue;
                }

                foreach (int col in columns.GetViewBetween(firstCol, lastCol - 1))
                {
                    double x = offsetX + col * cellSize;
                    double y = offsetY + row * cellSize;

                    dc.DrawRectangle(AliveBrush, null, new Rect(x, y, cellSize, cellSize));
                }
            }
        }

        private void DrawMinifiedCells(DrawingContext dc, int firstRow, int lastRow, int firstCol, int lastCol, double offsetX, double offsetY, double cellSize, double canvasWidth, double canvasHeight)
        {
            int pixelWidth = Math.Max(1, (int)Math.Ceiling(canvasWidth));

            int pixelHeight = Math.Max(1, (int)Math.Ceiling(canvasHeight));

            var visiblePixels = new HashSet<long>();

            for (int row = firstRow; row < lastRow; row++)
            {
                if (!aliveCells.TryGetValue(row, out var columns))
                {
                    continue;
                }

                foreach (int col in columns.GetViewBetween(firstCol, lastCol - 1))
                {
                    int px = (int)Math.Floor(offsetX + col * cellSize);

                    int py = (int)Math.Floor(offsetY + row * cellSize);

                    if (px < 0 || px >= pixelWidth || py < 0 || py >= pixelHeight)
                    {
                        continue;
                    }

                    visiblePixels.Add((long)py * pixelWidth + px);
                }
            }

            foreach (long pixel in visiblePixels)
            {
                int px = (int)(pixel % pixelWidth);
                int py = (int)(pixel / pixelWidth);

                dc.DrawRectangle(AliveBrush, null, new Rect(px, py, 1, 1));
            }
        }

        private void DrawGrid(DrawingContext dc, int firstRow, int lastRow, int firstCol, int lastCol, double offsetX, double offsetY, double cellSize, double canvasWidth, double canvasHeight)
        {
            double left = Math.Max(0, offsetX + firstCol * cellSize);

            double right = Math.Min(canvasWidth, offsetX + lastCol * cellSize);

            double top = Math.Max(0, offsetY + firstRow * cellSize);

            double bottom = Math.Min(canvasHeight, offsetY + lastRow * cellSize);

            for (int col = firstCol; col <= lastCol; col++)
            {
                double x = offsetX + col * cellSize;

                if (x >= 0 && x <= canvasWidth)
                {
                    dc.DrawLine(GridPen, new Point(x, top), new Point(x, bottom));
                }
            }

            for (int row = firstRow; row <= lastRow; row++)
            {
                double y = offsetY + row * cellSize;

                if (y >= 0 && y <= canvasHeight)
                {
                    dc.DrawLine(GridPen, new Point(left, y), new Point(right, y));
                }
            }
        }
    }
}
