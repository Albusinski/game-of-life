using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace game_of_life
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly CommandHandler commandHandler = new();
        private readonly BoardRenderer boardRenderer = new();
        private bool[,]? cells;
        private bool isPanning;
        private bool hasDragged;
        private Point lastMousePosition;
        private Point mouseDownPosition;
        private double zoom = 1.0;
        private double panX;
        private double panY;
        private const double ZoomFactor = 1.2;
        private const double MinZoom = 0.01;
        private const double MaxZoom = 500;
        private static readonly Color BackgroundColor = Color.FromRgb(0x18, 0x1A, 0x20);
        private static readonly Color AliveColor = Color.FromRgb(0x75, 0xD6, 0x9C);
        private static readonly Color GridColor = Color.FromRgb(0x37, 0x3C, 0x48);

        public MainWindow()
        {
            InitializeComponent();

            commandHandler.GridRequested += CreateGrid;

            GameCanvas.Children.Clear();

            boardRenderer.Width = GameCanvas.ActualWidth;
            boardRenderer.Height = GameCanvas.ActualHeight;

            GameCanvas.Children.Add(boardRenderer);

            Canvas.SetLeft(boardRenderer, 0);
            Canvas.SetTop(boardRenderer, 0);

            GameCanvas.SizeChanged += (_, _) =>
            {
                boardRenderer.Width = GameCanvas.ActualWidth;
                boardRenderer.Height = GameCanvas.ActualHeight;
                boardRenderer.InvalidateVisual();
            };
        }

        private void CommandInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            commandHandler.Execute(CommandInput.Text);
            CommandInput.Clear();

            e.Handled = true;
        }

        private void CreateGrid(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            cells = new bool[height, width];

            zoom = 1.0;
            panX = 0;
            panY = 0;

            boardRenderer.Width = GameCanvas.ActualWidth;
            boardRenderer.Height = GameCanvas.ActualHeight;
            boardRenderer.SetCells(cells);
            boardRenderer.SetView(zoom, panX, panY);
        }

        private void GameCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (cells == null)
            {
                return;
            }

            Point mouse = e.GetPosition(GameCanvas);

            double oldZoom = zoom;
            double newZoom = Math.Clamp(zoom * (e.Delta > 0 ? ZoomFactor : 1.0 / ZoomFactor), MinZoom, MaxZoom);

            if (newZoom == oldZoom)
            {
                return;
            }

            int width = cells.GetLength(1);
            int height = cells.GetLength(0);

            double baseCellSize = Math.Min(GameCanvas.ActualWidth / width, GameCanvas.ActualHeight / height);

            double offsetX = (GameCanvas.ActualWidth - width * baseCellSize) / 2;
            double offsetY = (GameCanvas.ActualHeight - height * baseCellSize) / 2;

            double gridX = (mouse.X - panX - offsetX * oldZoom) / (baseCellSize * oldZoom);

            double gridY = (mouse.Y - panY - offsetY * oldZoom) / (baseCellSize * oldZoom);

            zoom = newZoom;

            panX = mouse.X - offsetX * zoom - gridX * baseCellSize * zoom;
            panY = mouse.Y - offsetY * zoom - gridY * baseCellSize * zoom;

            boardRenderer.SetView(zoom, panX, panY);

            e.Handled = true;
        }

        private void GameCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isPanning = true;
            hasDragged = false;

            lastMousePosition = e.GetPosition(GameCanvas);
            mouseDownPosition = lastMousePosition;

            GameCanvas.CaptureMouse();
            e.Handled = true;
        }

        private void GameCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isPanning || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            Point currentPosition = e.GetPosition(GameCanvas);

            if ((currentPosition - mouseDownPosition).Length > 4)
            {
                hasDragged = true;
            }

            if (hasDragged)
            {
                Vector delta = currentPosition - lastMousePosition;

                panX += delta.X;
                panY += delta.Y;

                boardRenderer.SetView(zoom, panX, panY);
            }

            lastMousePosition = currentPosition;
        }

        private void GameCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!isPanning)
            {
                return;
            }

            if (!hasDragged)
            {
                SetCellAlive(e.GetPosition(GameCanvas));
            }

            isPanning = false;
            GameCanvas.ReleaseMouseCapture();

            e.Handled = true;
        }

        private void SetCellAlive(Point position)
        {
            if (cells == null)
            {
                return;
            }

            int width = cells.GetLength(1);
            int height = cells.GetLength(0);

            double baseCellSize = Math.Min(GameCanvas.ActualWidth / width, GameCanvas.ActualHeight / height);

            double cellSize = baseCellSize * zoom;

            if (cellSize <= 0)
            {
                return;
            }

            double offsetX = (GameCanvas.ActualWidth - width * baseCellSize) / 2 * zoom + panX;

            double offsetY = (GameCanvas.ActualHeight - height * baseCellSize) / 2 * zoom + panY;

            int col = (int)Math.Floor((position.X - offsetX) / cellSize);
            int row = (int)Math.Floor((position.Y - offsetY) / cellSize);

            if (row < 0 || row >= height || col < 0 || col >= width)
            {
                return;
            }
                
            if (cells[row, col])
            {
                return;
            }

            cells[row, col] = true;
            boardRenderer.RefreshAliveCells();
        }

        private sealed class BoardRenderer : FrameworkElement
        {
            private bool[,]? cells;
            private double zoom = 1.0;
            private double panX;
            private double panY;

            private readonly Dictionary<int, SortedSet<int>> aliveCells = new();

            private static readonly Brush BackgroundBrush = new SolidColorBrush(BackgroundColor);

            private static readonly Brush AliveBrush = new SolidColorBrush(AliveColor);

            private static readonly Pen GridPen = new Pen(new SolidColorBrush(GridColor), 1);

            public BoardRenderer()
            {
                ClipToBounds = true;
                Focusable = false;
            }

            public void SetCells(bool[,] newCells)
            {
                cells = newCells;
                RebuildAliveIndex();
                InvalidateVisual();
            }

            public void SetView(double newZoom, double newPanX, double newPanY)
            {
                zoom = newZoom;
                panX = newPanX;
                panY = newPanY;

                InvalidateVisual();
            }

            public void RefreshAliveCells()
            {
                RebuildAliveIndex();
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

                int width = cells.GetLength(1);
                int height = cells.GetLength(0);

                double baseCellSize = Math.Min(canvasWidth / width, canvasHeight / height);

                double cellSize = baseCellSize * zoom;

                if (cellSize <= 0)
                {
                    return;
                }

                double offsetX = (canvasWidth - width * baseCellSize) / 2 * zoom + panX;

                double offsetY = (canvasHeight - height * baseCellSize) / 2 * zoom + panY;

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
                            double x = offsetX + col * cellSize;
                            double y = offsetY + row * cellSize;

                            int px = (int)x;
                            int py = (int)y;

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
                else
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
}