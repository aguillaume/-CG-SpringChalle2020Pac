using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

class Player 
{
    private static Algo algo = new Algo();

    static void Main(string[] args)
    {
        algo.Play(args);
    }
}

internal class Algo : Logger
{
    Graph _gameGraph = new Graph();
    List<Pac> pacs = new List<Pac>();
    List<Pac> enemyPacs = new List<Pac>();
    Dictionary<Pac, string> _moveOutput = new Dictionary<Pac, string>();

    internal void Play(string[] args)
    {
        ReadMapInPut(args);

        while (true)
        {
            ReadInput(args);

            ClearPacInfo();
        }

        throw new NotImplementedException();
    }

    // remove the pcas from the graph so they can be added anew in the next turn input and not have duplicate pacs in the graph
    private void ClearPacInfo()
    {
        foreach (var pac in pacs.Concat(enemyPacs))
        {
            _gameGraph.FindNode(new Node(pac.Pos)).Pac = null;
        }
    }

    private void ReadMapInPut(string[] args)
    {
        string[] inputs;
        var inputBoardSize = Console.ReadLine();
        //Err($"inputBoardSize: {inputBoardSize}");
        inputs = inputBoardSize.Split(' ');
        int width = int.Parse(inputs[0]); // size of the grid
        int height = int.Parse(inputs[1]); // top left corner is (x=0, y=0)
        //_map = new Map(width, height);
        for (int y = 0; y < height; y++)
        {
            string row = Console.ReadLine(); // one line of the grid: space " " is floor, pound "#" is wall
            Node previousNode = null;
            Node wrapNode = null;
            for (int x = 0; x < row.Length; x++)
            {
                var isLast = x == row.Length - 1;
                var cr = row[x];
                if (cr == ' ')
                {
                    var node = new Node(new Pos(x, y))
                    {
                        Pellet = new Pellet()
                        {
                            Value = 1,
                            Pos = new Pos(x, y)
                        }
                    };

                    if (previousNode != null) _gameGraph.AddEdge(node, previousNode);
                    else _gameGraph.AddNode(node);

                    if (x == 0)
                    {
                        wrapNode = node;
                    }

                    if (isLast)
                    {
                        _gameGraph.AddEdge(node, wrapNode);
                        _gameGraph.AddEdge(wrapNode, node);
                    }

                    var aboveNeighbour = _gameGraph.FindNode(new Node( new Pos(x, y - 1)));
                    if (aboveNeighbour != null)
                    {
                        _gameGraph.AddEdge(node, aboveNeighbour);
                        _gameGraph.AddEdge(aboveNeighbour, node);
                    }

                    previousNode = node;
                }
                else previousNode = null;
            }
            //_map.PopulateMap(i, row);
            //Err($"inputRow: {row}");
        }
        //_pellets = _map.GetAllValidPos().Select(x => new Pellet { Pos = x, Value = 1 }).ToList();
        //List<Pac> pacs = new List<Pac>();
        //List<Pac> enemyPacs = new List<Pac>();
        throw new NotImplementedException();
    }

    void ReadInput(string[] args)
    {
        enemyPacs = new List<Pac>();
        var inputScore = Console.ReadLine();
        _moveOutput = new Dictionary<Pac, string>();
        //Err($"inputScore: {inputScore}");
        string[] inputs = inputScore.Split(' ');
        int myScore = int.Parse(inputs[0]);
        int opponentScore = int.Parse(inputs[1]);
        int visiblePacCount = int.Parse(Console.ReadLine()); // all your pacs and enemy pacs in sight
                                                             //Err($"visiblePacCount: {visiblePacCount}");
        var pacsAlive = new List<int>();
        for (int i = 0; i < visiblePacCount; i++)
        {
            var inputPacInfo = Console.ReadLine();
            Err($"inputPacInfo: {inputPacInfo}");
            inputs = inputPacInfo.Split(' ');
            var pacId = int.Parse(inputs[0]);// pac number (unique within a team)
            bool mine = inputs[1] != "0"; // true if this pac is yours
            int x = int.Parse(inputs[2]); // position in the grid
            int y = int.Parse(inputs[3]); // position in the grid
            string typeId = inputs[4]; // unused in wood leagues
            int speedTurnsLeft = int.Parse(inputs[5]); // unused in wood leagues
            var abilityCooldown = int.Parse(inputs[6]); // unused in wood leagues

            Pac pac = new Pac();
            if (mine)
            {
                pacsAlive.Add(pacId);
                var myPac = pacs.Find(p => p.Id == pacId);

                if (myPac == default)
                {
                    pacs.Add(new Pac()
                    {
                        Id = pacId,
                        Pos = new Pos(x, y),
                        PacType = Enum.Parse<PacType>(typeId),
                        SpeedTurnsLeft = speedTurnsLeft,
                        AbilityCooldown = abilityCooldown
                    });
                }
                else
                {
                    myPac.LastMove = myPac.Pos;
                    myPac.Pos = new Pos(x, y);
                    myPac.PacType = Enum.Parse<PacType>(typeId);
                    myPac.SpeedTurnsLeft = speedTurnsLeft;
                    myPac.AbilityCooldown = abilityCooldown;
                    if (myPac.Pos == myPac.NextTarget) myPac.NextTarget = null; // target has been reached
                }
                pac = pacs.Find(p => p.Id == pacId);
            }
            else
            {
                pac.Id = pacId;
                pac.Pos = new Pos(x, y);
                pac.PacType = Enum.Parse<PacType>(typeId);
                pac.SpeedTurnsLeft = speedTurnsLeft;
                pac.AbilityCooldown = abilityCooldown;
                enemyPacs.Add(pac);
            }

            _gameGraph.FindNode(new Node(pac.Pos)).Pac = pac;
        }
        // remove pacs that have died
        pacs.RemoveAll(p => !pacsAlive.Contains(p.Id));

        int visiblePelletCount = int.Parse(Console.ReadLine()); // all pellets in sight
        Err($"visiblePelletCount: {visiblePelletCount}");
        //List<Pellet> pellets = new List<Pellet>();
        List<Pellet> visiblePellets = new List<Pellet>();
        for (int i = 0; i < visiblePelletCount; i++)
        {
            var inputPelletInfo = Console.ReadLine();
            //Err($"inputPelletInfo: {inputPelletInfo}");
            inputs = inputPelletInfo.Split(' ');
            int x = int.Parse(inputs[0]);
            int y = int.Parse(inputs[1]);
            int value = int.Parse(inputs[2]); // amount of points this pellet is worth
                                              //pellets.Add(new Pellet { Pos = new Pos(x, y), Value = value });
            var pellet = new Pellet { Pos = new Pos(x, y), Value = value };
            visiblePellets.Add(pellet);
            _gameGraph.FindNode(new Node(new Pos(x, y))).Pellet = pellet;
        }

        throw new NotImplementedException();
    }
}

class Logger
{
    public void Err(string msg)
    {
        Console.Error.WriteLine(msg);
    }
}

internal class Graph
{
    private int _numberOfNodes;

    // Adjacency List Rappresentation of the graph
    private Dictionary<Node, List<Node>> _graph;

    internal Graph()
    {
        _graph = new Dictionary<Node, List<Node>>();
    }

    internal Node FindNode(Node node)
    {
        return _graph.ContainsKey(node) ? _graph.Keys.Single(k => k == node) : null;
    }

    internal void AddNode(Node node)
    {
        if (_graph.ContainsKey(node)) return;
        _graph[node] = new List<Node>();
    }

    internal void AddEdge(Node node, Node neightbour)
    {
        if (_graph[node] != null) _graph[node].Add(neightbour);
        else _graph[node] = new List<Node> { neightbour };
    }
}

class Node : IEquatable<Node>
{
    public int Id => Pos.GetHashCode();
    public Pos Pos { get; }
    public Pac Pac { get; set; }
    public Pellet Pellet { get; set; }

    public Node(Pos pos)
    {
        Pos = pos;
    }

    #region IEquatable
    public override bool Equals(object obj)
    {
        return Equals(obj as Node);
    }

    public bool Equals(Node other)
    {
        // If parameter is null, return false.
        if (ReferenceEquals(other, null)) return false;

        // Optimization for a common success case.
        if (ReferenceEquals(this, other)) return true;

        // If run-time types are not exactly the same, return false.
        if (GetType() != other.GetType()) return false;

        // Return true if the fields match.
        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode() * 17;
    }

    public static bool operator ==(Node lhs, Node rhs)
    {
        // Check for null on left side.
        if (ReferenceEquals(lhs, null))
        {
            if (ReferenceEquals(rhs, null))
            {
                // null == null = true.
                return true;
            }

            // Only the left side is null.
            return false;
        }

        // Equals handles case of null on right side.
        return lhs.Equals(rhs);
    }

    public static bool operator !=(Node lhs, Node rhs)
    {
        return !(lhs == rhs);
    }
    #endregion IEquatable

}

class Pos : IEquatable<Pos>
{
    public Pos(int x, int y)
    {
        X = x;
        Y = y;
    }

    public Pos(string pos)
    {
        var x = pos.Split(' ');
        X = int.Parse(x[0]);
        Y = int.Parse(x[1]);
    }

    public int X { get; set; }
    public int Y { get; set; }
    public Pos North => new Pos(X, Y - 1);
    public Pos East => new Pos(X + 1, Y);
    public Pos South => new Pos(X, Y + 1);
    public Pos West => new Pos(X - 1, Y);

    public override string ToString()
    {
        return $"{X} {Y}";
    }

    internal bool IsAdjacent(Pos pos) //(5, 5) >> (4, 5) | (6, 5) | (5, 4) | (5, 6)
    {
        if (X == pos.X + 1 && Y == pos.Y) return true;
        if (X == pos.X - 1 && Y == pos.Y) return true;
        if (X == pos.X && Y == pos.Y + 1) return true;
        if (X == pos.X && Y == pos.Y - 1) return true;
        return false;
    }

    #region IEquatable
    public override bool Equals(object obj)
    {
        return Equals(obj as Pos);
    }

    public bool Equals(Pos other)
    {
        // If parameter is null, return false.
        if (ReferenceEquals(other, null)) return false;

        // Optimization for a common success case.
        if (ReferenceEquals(this, other)) return true;

        // If run-time types are not exactly the same, return false.
        if (GetType() != other.GetType()) return false;

        // Return true if the fields match.
        return X == other.X && Y == other.Y;
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() * 17 + Y.GetHashCode();
    }

    public static bool operator ==(Pos lhs, Pos rhs)
    {
        // Check for null on left side.
        if (ReferenceEquals(lhs, null))
        {
            if (ReferenceEquals(rhs, null))
            {
                // null == null = true.
                return true;
            }

            // Only the left side is null.
            return false;
        }

        // Equals handles case of null on right side.
        return lhs.Equals(rhs);
    }

    public static bool operator !=(Pos lhs, Pos rhs)
    {
        return !(lhs == rhs);
    }
    #endregion IEquatable
}

class Pellet : IEquatable<Pellet>
{
    public Pos Pos { get; set; }
    public int Value { get; set; }

    public bool IsSuperPellet => Value == 10;

    public KeyValuePair<Pac, double>? ClosestPac => PacDistances?.OrderBy(x => x.Value)?.FirstOrDefault();

    public Dictionary<Pac, double> PacDistances { get; set; } = new Dictionary<Pac, double>();

    #region IEquatable
    public override bool Equals(object obj)
    {
        return Equals(obj as Pellet);
    }

    public bool Equals(Pellet other)
    {
        // If parameter is null, return false.
        if (ReferenceEquals(other, null)) return false;

        // Optimization for a common success case.
        if (ReferenceEquals(this, other)) return true;

        // If run-time types are not exactly the same, return false.
        if (GetType() != other.GetType()) return false;

        // Return true if the fields match.
        return Pos == other.Pos && Value == other.Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode() * 17 + Pos.GetHashCode();
    }

    public static bool operator ==(Pellet lhs, Pellet rhs)
    {
        // Check for null on left side.
        if (ReferenceEquals(lhs, null))
        {
            if (ReferenceEquals(rhs, null))
            {
                // null == null = true.
                return true;
            }

            // Only the left side is null.
            return false;
        }

        // Equals handles case of null on right side.
        return lhs.Equals(rhs);
    }

    public static bool operator !=(Pellet lhs, Pellet rhs)
    {
        return !(lhs == rhs);
    }
    #endregion IEquatable
}

class Pac : Logger, IEquatable<Pac>
{
    public int Id { get; set; }
    public Pos Pos { get; set; }
    public Pos NextTarget { get => _nextTarget; set => _nextTarget = value; }
    public Pos LastMove { get; set; }
    public bool IsBlocked => LastMove == Pos;
    public bool HasTarget => NextTarget != default;
    //KeyValuePair<nextTarget, Pos, nextmove>
    private Tuple<Pos, Pos, Pos> _nextMove = new Tuple<Pos, Pos, Pos>(null, null, null);
    private Pos _nextTarget;

    public Pos NextMove
    {
        get
        {
            if (!HasTarget)
            {
                return null;
            }

            if (_nextMove.Item1 == NextTarget && _nextMove.Item2 == Pos)
            {
                return _nextMove.Item3;
            }
            var pathFinder = new AStarPathFiner();
            Path = pathFinder.FindPath(_map, Pos, NextTarget);
            //Err($"Path [{Id}]: {Path.EnumerableToString()}");
            _nextMove = new Tuple<Pos, Pos, Pos>(NextTarget, Pos, Path.FirstOrDefault());
            return NextMove;
        }
        set
        {
            _nextMove = new Tuple<Pos, Pos, Pos>(NextTarget, Pos, value);
        }
    }

    public List<Pos> Path { get; set; }

    public PacType PacType { get; internal set; }
    public int SpeedTurnsLeft { get; internal set; }
    public int AbilityCooldown { get; internal set; }

    public bool IsOnSpeed => SpeedTurnsLeft != 0;

    public Pos Forward
    {
        get
        {
            if (NextMove == default) return new Pos(-1, -1);

            if (NextMove.Y - Pos.Y == -1) return Pos.North;
            if (NextMove.X - Pos.X == 1) return Pos.East;
            if (NextMove.Y - Pos.Y == 1) return Pos.South;
            if (NextMove.X - Pos.X == -1) return Pos.West;
            throw new Exception($"Forward nextMove - pos didn't do as expected.{this}.");
        }
    }

    public Pos DoubleForward
    {
        get
        {
            if (NextMove == default) return new Pos(-1, -1);

            if (NextMove.Y - Pos.Y == -1) return Pos.North.North;
            if (NextMove.X - Pos.X == 1) return Pos.East.East;
            if (NextMove.Y - Pos.Y == 1) return Pos.South.South;
            if (NextMove.X - Pos.X == -1) return Pos.West.West;
            throw new Exception($"DoubleForward nextMove - pos didn't do as expected. {this}.");
        }
    }

    public override string ToString()
    {
        return $"Id:{Id},[{Pos}],{PacType},target:[{NextTarget}],nextMove[{NextMove}],IsBlocked:{IsBlocked},lastMove:{LastMove}";
    }

    #region IEquatable
    public override bool Equals(object obj)
    {
        return Equals(obj as Pac);
    }

    public bool Equals(Pac other)
    {
        // If parameter is null, return false.
        if (ReferenceEquals(other, null)) return false;

        // Optimization for a common success case.
        if (ReferenceEquals(this, other)) return true;

        // If run-time types are not exactly the same, return false.
        if (GetType() != other.GetType()) return false;

        // Return true if the fields match.
        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode() * 17;
    }

    public static bool operator ==(Pac lhs, Pac rhs)
    {
        // Check for null on left side.
        if (ReferenceEquals(lhs, null))
        {
            if (ReferenceEquals(rhs, null))
            {
                // null == null = true.
                return true;
            }

            // Only the left side is null.
            return false;
        }

        // Equals handles case of null on right side.
        return lhs.Equals(rhs);
    }

    public static bool operator !=(Pac lhs, Pac rhs)
    {
        return !(lhs == rhs);
    }
    #endregion IEquatable
}

public enum PacType
{
    ROCK,
    PAPER,
    SCISSORS
}