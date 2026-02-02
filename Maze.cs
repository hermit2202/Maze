using System;
using System.Collections.Generic;
using System.Text;

// Класс для представления точки
public struct Point
{
    public int X { get; set; }
    public int Y { get; set; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override bool Equals(object obj)
    {
        if (obj is Point other)
            return X == other.X && Y == other.Y;
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }
}

// Класс для конфигурации игры
public class GameConfig
{
    public char WallChar { get; set; } = '█';
    public char PathChar { get; set; } = '·';
    public char PlayerChar { get; set; } = 'P';
    public char ExitChar { get; set; } = 'E';
    public char EmptyChar { get; set; } = ' ';
    public bool ShowAnimations { get; set; } = true;
    public ConsoleColor WallColor { get; set; } = ConsoleColor.DarkBlue;
    public ConsoleColor PathColor { get; set; } = ConsoleColor.Yellow;
    public ConsoleColor PlayerColor { get; set; } = ConsoleColor.Green;
    public ConsoleColor ExitColor { get; set; } = ConsoleColor.Red;
}

// Класс для статистики игры
public class GameStats
{
    public int Score { get; set; }
    public TimeSpan TimeElapsed { get; set; }
    public int MovesCount { get; set; }
    public DateTime StartTime { get; set; }
    
    public void StartTimer()
    {
        StartTime = DateTime.Now;
    }
    
    public void UpdateTimer()
    {
        TimeElapsed = DateTime.Now - StartTime;
    }
    
    public void CalculateScore(Difficulty difficulty)
    {
        int timeBonus = Math.Max(0, 1000 - (int)TimeElapsed.TotalSeconds * 5);
        int movesBonus = Math.Max(0, 500 - MovesCount * 2);
        int difficultyMultiplier = (int)difficulty / 5;
        
        Score = (timeBonus + movesBonus) * difficultyMultiplier;
    }
}

// Класс лабиринта
public class Maze
{
    public char[,] Grid { get; private set; }
    public int Width { get; }
    public int Height { get; }
    private Random random = new Random();

    public Maze(int width, int height)
    {
        Width = width;
        Height = height;
        Grid = new char[width, height];
        Generate();
    }

    public bool CanMove(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height && Grid[x, y] == ' ';
    }

    private void Generate()
    {
        // Заполняем всё стенами
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                Grid[x, y] = '█';

        // Используем алгоритм Prim's для генерации лабиринта
        PrimGenerate();

        // Создаем вход и выход
        Grid[1, 0] = ' ';
        Grid[Width - 2, Height - 1] = ' ';
    }

    private void PrimGenerate()
    {
        var frontiers = new List<Point>();
        int startX = 1;
        int startY = 1;
        Grid[startX, startY] = ' ';
        AddFrontiers(startX, startY, frontiers);

        while (frontiers.Count > 0)
        {
            int randomIndex = random.Next(frontiers.Count);
            Point frontier = frontiers[randomIndex];
            frontiers.RemoveAt(randomIndex);

            var neighbors = GetNeighborPassages(frontier.X, frontier.Y);

            if (neighbors.Count > 0)
            {
                var neighbor = neighbors[random.Next(neighbors.Count)];
                Grid[frontier.X, frontier.Y] = ' ';
                
                int betweenX = (frontier.X + neighbor.X) / 2;
                int betweenY = (frontier.Y + neighbor.Y) / 2;
                Grid[betweenX, betweenY] = ' ';
                
                AddFrontiers(frontier.X, frontier.Y, frontiers);
            }
        }
    }

    private void AddFrontiers(int x, int y, List<Point> frontiers)
    {
        int[] dx = { 0, 0, 2, -2 };
        int[] dy = { 2, -2, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int newX = x + dx[i];
            int newY = y + dy[i];

            if (newX > 0 && newX < Width - 1 && newY > 0 && newY < Height - 1 && 
                Grid[newX, newY] == '█')
            {
                var point = new Point(newX, newY);
                if (!frontiers.Contains(point))
                    frontiers.Add(point);
            }
        }
    }

    private List<Point> GetNeighborPassages(int x, int y)
    {
        var neighbors = new List<Point>();
        int[] dx = { 0, 0, 2, -2 };
        int[] dy = { 2, -2, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            int newX = x + dx[i];
            int newY = y + dy[i];

            if (newX > 0 && newX < Width - 1 && newY > 0 && newY < Height - 1 && 
                Grid[newX, newY] == ' ')
            {
                neighbors.Add(new Point(newX, newY));
            }
        }

        return neighbors;
    }
}

// Класс игрока
public class Player
{
    public int X { get; private set; }
    public int Y { get; private set; }

    public Player(int startX, int startY)
    {
        X = startX;
        Y = startY;
    }

    public bool Move(int dx, int dy, Maze maze)
    {
        int newX = X + dx;
        int newY = Y + dy;

        if (maze.CanMove(newX, newY))
        {
            X = newX;
            Y = newY;
            return true;
        }
        return false;
    }

    public void SetPosition(int x, int y)
    {
        X = x;
        Y = y;
    }
}

// Класс для поиска пути
public class PathFinder
{
    public List<Point> FindShortestPath(Maze maze, Point start, Point end)
    {
        int width = maze.Width;
        int height = maze.Height;
        
        var queue = new Queue<Point>();
        var visited = new bool[width, height];
        var parent = new Point[width, height];
        
        queue.Enqueue(start);
        visited[start.X, start.Y] = true;

        int[] dx = { 0, 1, 0, -1 };
        int[] dy = { -1, 0, 1, 0 };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.X == end.X && current.Y == end.Y)
            {
                var path = new List<Point>();
                var point = current;
                
                while (point.X != start.X || point.Y != start.Y)
                {
                    path.Add(point);
                    point = parent[point.X, point.Y];
                }
                
                path.Reverse();
                return path;
            }

            for (int i = 0; i < 4; i++)
            {
                int newX = current.X + dx[i];
                int newY = current.Y + dy[i];

                if (newX >= 0 && newX < width && newY >= 0 && newY < height && 
                    !visited[newX, newY] && maze.Grid[newX, newY] == ' ')
                {
                    visited[newX, newY] = true;
                    parent[newX, newY] = current;
                    queue.Enqueue(new Point(newX, newY));
                }
            }
        }

        return new List<Point>();
    }
}

// Перечисление сложности
public enum Difficulty
{
    Easy = 15,
    Medium = 21,
    Hard = 31
}

// Главный класс программы
class Program
{
    private static Maze maze;
    private static Player player;
    private static PathFinder pathFinder;
    private static GameStats stats;
    private static GameConfig config;
    private static Point exit;
    private static bool showPath = false;
    private static Difficulty currentDifficulty = Difficulty.Medium;

    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false; // Скрываем курсор для уменьшения мерцания
        Console.Title = "Генератор лабиринтов";
        
        config = new GameConfig();
        pathFinder = new PathFinder();
        
        ShowMainMenu();
    }

    // Главное меню
    static void ShowMainMenu()
    {
        Console.CursorVisible = true; // В меню курсор видим
        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════╗");
            Console.WriteLine("║       ГЕНЕРАТОР ЛАБИРИНТОВ       ║");
            Console.WriteLine("╚══════════════════════════════════╝");
            Console.ResetColor();
            
            Console.WriteLine("\n1. Новая игра");
            Console.WriteLine("2. Выбрать сложность");
            Console.WriteLine("3. Выход");
            Console.Write("\nВыберите вариант: ");

            var key = Console.ReadKey(true).Key;
            switch (key)
            {
                case ConsoleKey.D1: StartNewGame(); break;
                case ConsoleKey.D2: ShowDifficultyMenu(); break;
                case ConsoleKey.D3: 
                    Console.CursorVisible = true;
                    return;
            }
        }
    }

    static void ShowDifficultyMenu()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("╔══════════════════════════════════╗");
        Console.WriteLine("║         ВЫБОР СЛОЖНОСТИ          ║");
        Console.WriteLine("╚══════════════════════════════════╝");
        Console.ResetColor();
        
        Console.WriteLine($"\nТекущая сложность: {currentDifficulty}");
        Console.WriteLine("\n1. Легкая (15x15)");
        Console.WriteLine("2. Средняя (21x21)");
        Console.WriteLine("3. Сложная (31x31)");
        Console.WriteLine("4. Назад");
        Console.Write("\nВыберите вариант: ");

        var key = Console.ReadKey(true).Key;
        switch (key)
        {
            case ConsoleKey.D1: currentDifficulty = Difficulty.Easy; break;
            case ConsoleKey.D2: currentDifficulty = Difficulty.Medium; break;
            case ConsoleKey.D3: currentDifficulty = Difficulty.Hard; break;
            case ConsoleKey.D4: return;
        }
        
        Console.WriteLine($"\nСложность изменена на: {currentDifficulty}");
        Console.WriteLine("Нажмите любую клавишу для возврата...");
        Console.ReadKey();
    }

    static void StartNewGame()
    {
        Console.CursorVisible = false; // В игре скрываем курсор
        int size = (int)currentDifficulty;
        
        // Создание объектов
        maze = new Maze(size, size);
        stats = new GameStats();
        stats.StartTimer();
        
        PlacePlayerAndExit();
        
        Console.Clear();
        DrawMaze();
        DrawStats();
        
        Console.WriteLine("Нажмите любую клавишу для начала...");
        Console.ReadKey();

        GameLoop();
    }

    static void PlacePlayerAndExit()
    {
        int centerX = maze.Width / 2;
        int centerY = maze.Height / 2;
        
        // Находим ближайшую свободную клетку для игрока
        FindNearestEmptyCell(ref centerX, ref centerY);
        player = new Player(centerX, centerY);

        // Выход в правом нижнем углу
        int exitX = maze.Width - 2;
        int exitY = maze.Height - 2;
        FindNearestEmptyCell(ref exitX, ref exitY);
        exit = new Point(exitX, exitY);
    }

    static void FindNearestEmptyCell(ref int x, ref int y)
    {
        if (maze.CanMove(x, y)) return;

        for (int radius = 1; radius < Math.Max(maze.Width, maze.Height); radius++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int newX = x + dx;
                    int newY = y + dy;
                    
                    if (maze.CanMove(newX, newY))
                    {
                        x = newX;
                        y = newY;
                        return;
                    }
                }
            }
        }
    }

    static void GameLoop()
    {
        bool needsRedraw = true;
        int oldPlayerX = player.X;
        int oldPlayerY = player.Y;
        
        while (true)
        {
            stats.UpdateTimer();
            
            // Перерисовываем только при необходимости
            if (needsRedraw)
            {
                Console.Clear();
                DrawMaze();
                DrawStats();
                needsRedraw = false;
            }
            else
            {
                // Обновляем только статистику без перерисовки всего экрана
                UpdateStats();
            }

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.Q) 
                {
                    ShowMainMenu();
                    return;
                }
                if (key == ConsoleKey.R) 
                {
                    StartNewGame();
                    return;
                }
                if (key == ConsoleKey.X) 
                {
                    showPath = !showPath;
                    needsRedraw = true; // Требуется полная перерисовка
                    continue;
                }

                // Движение игрока
                bool moved = false;
                oldPlayerX = player.X;
                oldPlayerY = player.Y;
                
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.W:
                        moved = player.Move(0, -1, maze);
                        break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.S:
                        moved = player.Move(0, 1, maze);
                        break;
                    case ConsoleKey.LeftArrow:
                    case ConsoleKey.A:
                        moved = player.Move(-1, 0, maze);
                        break;
                    case ConsoleKey.RightArrow:
                    case ConsoleKey.D:
                        moved = player.Move(1, 0, maze);
                        break;
                }

                if (moved)
                {
                    stats.MovesCount++;
                    // Обновляем только позицию игрока
                    UpdatePlayerPosition(oldPlayerX, oldPlayerY, player.X, player.Y);
                }

                // Проверка победы
                if (player.X == exit.X && player.Y == exit.Y)
                {
                    stats.CalculateScore(currentDifficulty);
                    ShowWinScreen();
                    return;
                }
            }
            
            // Небольшая задержка для уменьшения нагрузки на CPU
            System.Threading.Thread.Sleep(16);
        }
    }

    // Обновляет только позицию игрока
    static void UpdatePlayerPosition(int oldX, int oldY, int newX, int newY)
    {
        // Сохраняем текущую позицию курсора
        int originalLeft = Console.CursorLeft;
        int originalTop = Console.CursorTop;
        
        try
        {
            // Очищаем старую позицию игрока
            Console.SetCursorPosition(oldX * 2 + 1, oldY + 1);
            if (maze.Grid[oldX, oldY] == ' ')
            {
                Console.Write("  ");
            }
            else
            {
                Console.ForegroundColor = config.WallColor;
                Console.Write(config.WallChar + " ");
                Console.ResetColor();
            }
            
            // Рисуем игрока на новой позиции
            Console.SetCursorPosition(newX * 2 + 1, newY + 1);
            Console.ForegroundColor = config.PlayerColor;
            Console.Write(config.PlayerChar + " ");
            Console.ResetColor();
        }
        finally
        {
            // Всегда возвращаем курсор в исходное положение
            Console.SetCursorPosition(originalLeft, originalTop);
        }
    }

    // Обновляет только статистику
    static void UpdateStats()
    {
        // Сохраняем позицию курсора
        int cursorLeft = Console.CursorLeft;
        int cursorTop = Console.CursorTop;
        
        // Переходим к строке со статистикой
        Console.SetCursorPosition(0, maze.Height + 3);
        
        // Очищаем строку со статистикой
        Console.Write(new string(' ', Console.WindowWidth - 1));
        Console.SetCursorPosition(0, maze.Height + 3);
        
        // Перерисовываем статистику
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"Ходы: {stats.MovesCount} | Время: {stats.TimeElapsed:mm\\:ss} | Сложность: {currentDifficulty}");
        
        if (showPath)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" | Путь: ВКЛ");
        }
        
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("Управление: ←↑↓→/WASD - движение, X - путь, R - рестарт, Q - выход");
        Console.ResetColor();
        
        // Восстанавливаем позицию курсора
        Console.SetCursorPosition(cursorLeft, cursorTop);
    }

    // Улучшенная отрисовка с рамкой
    static void DrawMaze()
    {
        List<Point> path = null;
        if (showPath)
            path = pathFinder.FindShortestPath(maze, new Point(player.X, player.Y), exit);

        // Верхняя рамка
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("╔");
        for (int i = 0; i < maze.Width * 2; i++) Console.Write("═");
        Console.WriteLine("╗");
        
        for (int y = 0; y < maze.Height; y++)
        {
            Console.Write("║");
            for (int x = 0; x < maze.Width; x++)
            {
                char symbol = maze.Grid[x, y];
                
                if (x == player.X && y == player.Y)
                {
                    Console.ForegroundColor = config.PlayerColor;
                    Console.Write(config.PlayerChar + " ");
                }
                else if (x == exit.X && y == exit.Y)
                {
                    Console.ForegroundColor = config.ExitColor;
                    Console.Write(config.ExitChar + " ");
                }
                else if (showPath && path != null && path.Contains(new Point(x, y)))
                {
                    Console.ForegroundColor = config.PathColor;
                    Console.Write(config.PathChar + " ");
                }
                else if (symbol == '█')
                {
                    Console.ForegroundColor = config.WallColor;
                    Console.Write(config.WallChar + " ");
                }
                else
                {
                    Console.Write("  ");
                }
                Console.ResetColor();
            }
            Console.WriteLine("║");
        }
        
        // Нижняя рамка
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("╚");
        for (int i = 0; i < maze.Width * 2; i++) Console.Write("═");
        Console.WriteLine("╝");
        Console.ResetColor();
    }

    // Отображение статистики (для первоначальной отрисовки)
    static void DrawStats()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\nХоды: {stats.MovesCount} | Время: {stats.TimeElapsed:mm\\:ss} | Сложность: {currentDifficulty}");
        
        if (showPath)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Кратчайший путь показан желтыми точками");
        }
        
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("Управление: ←↑↓→/WASD - движение, X - путь, R - рестарт, Q - выход");
        Console.ResetColor();
    }

    static void ShowWinScreen()
    {
        Console.Clear();
        DrawMaze();
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n╔══════════════════════════════════╗");
        Console.WriteLine("║        ПОЗДРАВЛЯЕМ!              ║");
        Console.WriteLine("║    ВЫ ВЫШЛИ ИЗ ЛАБИРИНТА!        ║");
        Console.WriteLine("╚══════════════════════════════════╝");
        Console.ResetColor();
        
        Console.WriteLine($"\nВаш результат:");
        Console.WriteLine($"Время: {stats.TimeElapsed:mm\\:ss}");
        Console.WriteLine($"Ходы: {stats.MovesCount}");
        Console.WriteLine($"Сложность: {currentDifficulty}");
        Console.WriteLine($"Очки: {stats.Score}");
        
        Console.WriteLine("\nНажмите любую клавишу для возврата в меню...");
        Console.ReadKey();
    }
}