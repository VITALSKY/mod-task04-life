using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.IO;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }
    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;
        public int AliveCellsCounter;
        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        [JsonConstructor]
        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        public Board(List<List<bool>> board)
        {
            CellSize = 1;
            AliveCellsCounter = 0;
            Cells = new Cell[board[0].Count, board.Count];
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    Cells[x, y] = new Cell() { IsAlive = board[y][x] };
                    AliveCellsCounter += Convert.ToInt32(board[y][x]);
                }
            }
            ConnectNeighbors();
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            AliveCellsCounter = 0;
            foreach (var cell in Cells)
            {
                cell.IsAlive = rand.NextDouble() < liveDensity;
                AliveCellsCounter += Convert.ToInt32(cell.IsAlive);
            }
        }

        public void Advance()
        {
            AliveCellsCounter = 0;
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
            {
                cell.Advance();
                AliveCellsCounter += Convert.ToInt32(cell.IsAlive);
            }
        }
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }

        public string INTO_STRING()
        {
            string result = "";
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    var cell = Cells[col, row];
                    if (cell.IsAlive)
                        result += "*";
                    else
                        result += " ";
                }
                result += "\n";
            }
            return result;
        }
    }
    class Loader
    {
        public static void toFile(Board board, string filepath)
        {
            string content = board.INTO_STRING();
            StreamWriter sw = new StreamWriter(filepath);
            sw.Write(content);
            sw.Close();
        }
        public static Board fromFile(string filepath)
        {
            StreamReader sr = new StreamReader(filepath);
            var board = new List<List<bool>>() { new List<bool>() };

            int i = 0;
            while (!sr.EndOfStream)
            {
                char cell = Convert.ToChar(sr.Read());
                if (cell == '\n')
                {
                    if (!sr.EndOfStream)
                    {
                        board.Add(new List<bool>());
                        i++;
                    }
                }
                else if (cell == ' ' || cell == '\r')
                {
                    board[i].Add(false);
                }
                else
                {
                    board[i].Add(true);
                }
            }
            sr.Close();
            return new Board(board);
        }
    }
    class Program
    {
        static private bool save = false;
        static Board board;
        static private void Reset()
        {
            board = File.Exists("options.json") ? JsonConvert.DeserializeObject<Board>(File.ReadAllText("options.json")) : new Board(
                width: 50,
                height: 20,
                cellSize: 1,
                liveDensity: 0.5);
        }
        static void Render()
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    var cell = board.Cells[col, row];
                    if (cell.IsAlive)
                    {
                        Console.Write('*');
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.Write('\n');
            }
        }
        public static void funct()
        {
            while (true)
            {
                ConsoleKeyInfo cons = Console.ReadKey();
                if (cons.Key == ConsoleKey.Escape)
                    save = true;
            }
        }
        static void Main(string[] args)
        {
            string filepath = "";
            Console.WriteLine("Press R to load from file\n Press I to rebuild\n");
            ConsoleKeyInfo cons = Console.ReadKey();
            if (cons.Key == ConsoleKey.R)
            {
                Console.Clear();
                Console.WriteLine("Read file name: ");
                filepath = Console.ReadLine();
                board = Loader.fromFile(filepath);
            }
            if (cons.Key == ConsoleKey.I)
            {
                Reset();
            }
            Thread waitKey = new Thread(new ThreadStart(Program.funct));
            waitKey.Start();
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Press ESC to save and exit");
                Render();
                if (save)
                {
                    Console.Clear();
                    Console.WriteLine("Read file name: ");
                    filepath = Console.ReadLine();
                    Loader.toFile(board, filepath);
                    System.Environment.Exit(0);
                }
                Console.WriteLine("Number of Alive Cells: " + board.AliveCellsCounter);
                board.Advance();
                Thread.Sleep(1000);
            }
        }
    }
}