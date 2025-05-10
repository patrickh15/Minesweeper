using System.Data.Common;
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
using System.Windows.Threading;

namespace Minesweeper
{
    public partial class MainWindow : Window
    {
        const int EASY_HEIGHT = 10;
        const int EASY_WIDTH = 10;
        const int EASY_MINES = 10;

        const int VALUE_FOR_MINE = -1;

        private enum CellState { Hidden, Shown, Flagged };
        private enum GameState { Running, Lost, Won};

        private int[,] gameMatrix;
        private CellState[,] gameStateMatrix;
        
        
        Button[,] gameButtons;

        Random random;

        DispatcherTimer timer;
        int seconds = 0;
        int minesMarked = 0;

        public MainWindow()
        {
            InitializeComponent();
            random = new Random();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += addSecond;

            resetGame();
            
        }

        private void resetGame()
        {
            timer.Stop();
            seconds = 0;
            initGameAndStateMatrix();
            initGameUI();
            draw();
            timer.Start();
        }

        private void addSecond(object sender, EventArgs e)
        {
            seconds++;
            labelTimer.Content = "Sekunden: " + seconds;
        }

        private void initGameAndStateMatrix()
        {
            gameMatrix = new int[EASY_HEIGHT, EASY_WIDTH];
            gameStateMatrix = new CellState[gameMatrix.GetLength(0), gameMatrix.GetLength(1)];
            

            for(int i = 0; i < gameMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < gameMatrix.GetLength(1); j++)
                {
                    gameMatrix[i, j] = 0;
                    gameStateMatrix[i, j] = CellState.Hidden;
                }   
            }

            //add mines
            int minesAdded = 0;
            do
            {
                int newTop = random.Next(0, gameMatrix.GetLength(0));
                int newLeft = random.Next(0, gameMatrix.GetLength(1));

                if(gameMatrix[newTop, newLeft] == 0)
                {
                    gameMatrix[newTop, newLeft] = VALUE_FOR_MINE;
                    minesAdded++;
                }
            } while (minesAdded < EASY_MINES);

            for (int i = 0; i < gameMatrix.GetLength(0); i++)
                for (int j = 0; j < gameMatrix.GetLength(1); j++)
                    if (gameMatrix[i, j] != VALUE_FOR_MINE)
                        gameMatrix[i, j] = countMinesAround(i, j);
        }

        private int countMinesAround(int row, int col)
        {
            int minesAround = 0;
            bool topRow = row == 0;
            bool bottomRow = row == (gameMatrix.GetLength(0) - 1);
            bool leftRow = col == 0;
            bool rightRow = col == (gameMatrix.GetLength(1) - 1);

            if(!leftRow && !topRow) //oben links
                if (gameMatrix[row - 1, col - 1] == VALUE_FOR_MINE) 
                    minesAround++;
            
            if(!topRow) //oben mitte
                if (gameMatrix[row - 1, col] == VALUE_FOR_MINE)
                    minesAround++;

            if (!topRow && !rightRow) //oben rechts
                if (gameMatrix[row -1, col + 1] == VALUE_FOR_MINE)
                    minesAround++;

            if (!rightRow) //mitte rechts
                if (gameMatrix[row, col + 1] == VALUE_FOR_MINE)
                    minesAround++;

            if(!bottomRow && !rightRow) //unten rechts
                if (gameMatrix[row + 1, col + 1] == VALUE_FOR_MINE)
                    minesAround++;

            if (!bottomRow) // unten mitte
                if (gameMatrix[row + 1, col] == VALUE_FOR_MINE)
                    minesAround++;

            if (!bottomRow && !leftRow) // unten links
                if (gameMatrix[row + 1, col - 1] == VALUE_FOR_MINE)
                    minesAround++;

            if(!leftRow) // mitte links
                if (gameMatrix[row, col - 1] == VALUE_FOR_MINE)
                    minesAround++;

            return minesAround;
        }

        private void initGameUI()
        {
            gameButtons = new Button[gameMatrix.GetLength(0), gameMatrix.GetLength(1)];

            gridGame.RowDefinitions.Clear();
            for (int i = 0; i < gameMatrix.GetLength(0); i++)
                gridGame.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            gridGame.ColumnDefinitions.Clear();
            for (int i = 0; i < gameMatrix.GetLength(1); i++)
                gridGame.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });


            for (int i = 0; i < gameButtons.GetLength(0); i++)
            {
                for (int j = 0; j < gameButtons.GetLength(1); j++)
                {
                    gameButtons[i, j] = new Button()
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Background = Brushes.LightGray,
                        Tag = (i, j) //kann in den events wieder ausgelesen werden, damit man weiß, welcher geklickt wurde
                    };

                    gameButtons[i, j].Click += buttonClick;
                    gameButtons[i, j].MouseRightButtonDown += buttonRightClick;

                    Grid.SetRow(gameButtons[i, j], i);
                    Grid.SetColumn(gameButtons[i, j], j);
                    gridGame.Children.Add(gameButtons[i, j]);
                }
            }
        }

        private void buttonClick(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            (int row, int col) = ((int,int))button.Tag;
            gameStateMatrix[row, col] = CellState.Shown;
            if (gameMatrix[row, col] == VALUE_FOR_MINE) //auf mine geklickt --> verloren
                endGame(false);
            else
                unveilEmptyFields(row, col);

            if(playerWon())
                endGame(true);

            draw();
        }

        private void buttonRightClick(object sender, MouseButtonEventArgs e)
        {
            Button button = (Button)sender;
            (int row, int col) = ((int, int))button.Tag;
            if(gameStateMatrix[row, col] == CellState.Hidden)
            {
                gameStateMatrix[row, col] = CellState.Flagged;
                minesMarked++;
            }
            else if (gameStateMatrix[row, col] == CellState.Flagged)
            {
                gameStateMatrix[row, col] = CellState.Hidden;
                minesMarked--;
            }
            updateMineCounter();
            draw();
        }

        private void unveilEmptyFields(int row, int col)
        {
            
            gameStateMatrix[row, col] = CellState.Shown;

            bool topRow = row == 0;
            bool bottomRow = row == (gameMatrix.GetLength(0) - 1);
            bool leftRow = col == 0;
            bool rightRow = col == (gameMatrix.GetLength(1) - 1);

            if (gameMatrix[row, col] == 0)
            {
                if (!leftRow && !topRow && gameStateMatrix[row - 1, col - 1] == CellState.Hidden) //oben links 
                    unveilEmptyFields(row - 1, col - 1);

                if (!topRow && gameStateMatrix[row - 1, col] == CellState.Hidden) //oben mitte
                    unveilEmptyFields(row - 1, col);

                if (!topRow && !rightRow && gameStateMatrix[row - 1, col + 1] == CellState.Hidden) //oben rechts
                    unveilEmptyFields(row - 1, col + 1);

                if (!rightRow && gameStateMatrix[row, col + 1] == CellState.Hidden) //mitte rechts
                    unveilEmptyFields(row, col + 1);

                if (!bottomRow && !rightRow && gameStateMatrix[row + 1, col + 1] == CellState.Hidden) //unten rechts
                    unveilEmptyFields(row + 1, col + 1);

                if (!bottomRow && gameStateMatrix[row + 1, col] == CellState.Hidden) // unten mitte
                    unveilEmptyFields(row + 1, col);

                if (!bottomRow && !leftRow && gameStateMatrix[row + 1, col - 1] == CellState.Hidden) // unten links
                    unveilEmptyFields(row + 1, col - 1);

                if (!leftRow && gameStateMatrix[row, col - 1] == CellState.Hidden) // mitte links
                    unveilEmptyFields(row, col - 1);
            }
        }

        private bool playerWon()
        {   
            bool won = true;
            for (int i = 0; i < EASY_HEIGHT; i++)
                for (int j = 0; j < EASY_WIDTH; j++)
                    if (gameMatrix[i, j] != VALUE_FOR_MINE && gameStateMatrix[i,j] == CellState.Hidden)
                        won = false;

            return won;
        }

        private void endGame(bool won)
        {
            // todo
            if (won)
                MessageBox.Show("Gewonnen");
            else
                MessageBox.Show("Verloren");

            resetGame();
        }

        private void updateMineCounter()
        {
            labelMines.Content = "Minen: " + minesMarked;
        }
        private void draw()
        {
            for(int i = 0;i < gameButtons.GetLength(0);i++)
            {
                for (int j = 0; j < gameButtons.GetLength(1); j++)
                {
                    if (gameStateMatrix[i, j] == CellState.Hidden)
                    {
                        gameButtons[i, j].Background = Brushes.LightGray;
                        gameButtons[i, j].Content = "";
                    }
                    else if (gameStateMatrix[i, j] == CellState.Flagged)
                    {
                        gameButtons[i, j].Background = Brushes.LightGray;
                        gameButtons[i, j].Content = "🚩";
                    } 
                    else if (gameStateMatrix[i,j] == CellState.Shown)
                    {
                        if (gameMatrix[i, j] == VALUE_FOR_MINE)
                        {
                            gameButtons[i, j].Background = Brushes.Red;
                            gameButtons[i, j].Content = "💥";
                        }
                        else
                        {
                            gameButtons[i, j].Background = Brushes.White;
                            gameButtons[i, j].Content = gameMatrix[i, j] == 0 ? "" : gameMatrix[i, j];
                        }
                        
                    }
                }
            }
            
                
        }
    }
}