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
        private Engine? engine;
        //private bool[,]? cells;
        private bool isPanning;
        private bool hasDragged;
        private Point lastMousePosition;
        private Point mouseDownPosition;
        //private double zoom = 1.0;
        //private double panX;
        //private double panY;
        private const double ZoomFactor = 1.2;
        private const double MinZoom = 0.01;
        private const double MaxZoom = 500;
        //private static readonly Color BackgroundColor = Color.FromRgb(0x18, 0x1A, 0x20);
        //private static readonly Color AliveColor = Color.FromRgb(0x75, 0xD6, 0x9C);
        //private static readonly Color GridColor = Color.FromRgb(0x37, 0x3C, 0x48);

        public MainWindow()
        {
            InitializeComponent();

            commandHandler.GridRequested += CreateGrid;

            GameCanvas.Children.Clear();
            GameCanvas.Children.Add(boardRenderer);

            Canvas.SetLeft(boardRenderer, 0);
            Canvas.SetTop(boardRenderer, 0);

            UpdateRendererSize();

            GameCanvas.SizeChanged += (_, _) =>
            {
                
                UpdateRendererSize();
            };
        }
        
        private void UpdateRendererSize()
        {
            boardRenderer.Width = GameCanvas.ActualWidth;
            boardRenderer.Height = GameCanvas.ActualHeight;
            boardRenderer.InvalidateVisual();
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

            engine = new Engine(width, height);

            boardRenderer.SetCells(engine.Cells);
            boardRenderer.ResetView();
        }

        private void GameCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (engine == null)
            {
                return;
            }

            Point mouse = e.GetPosition(GameCanvas);

            double factor = e.Delta > 0 ? ZoomFactor : 1.0 / ZoomFactor;

            boardRenderer.ZoomAt(mouse, factor, MinZoom, MaxZoom);

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
                boardRenderer.PanBy(delta);
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
                ToggleCell(e.GetPosition(GameCanvas));
            }

            isPanning = false;
            GameCanvas.ReleaseMouseCapture();

            e.Handled = true;
        }

        private void ToggleCell(Point position) //wcześniej SetCellAlive ale teraz jest toggle
        {
            if (engine == null)
            {
                return;
            }

            if (!boardRenderer.TryGetCellAt(
                position, out int row, out int col))
            {
                return;
            }

            bool alive = engine.ToggleCell(row, col);

            boardRenderer.SetCellState(row, col, alive);
        }
    }
}