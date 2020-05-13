using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;

/**
 * Grab the pellets as fast as you can!
 **/
class Player : Logger
{
    private static Game game = new Game();

    static void Main(string[] args)
    {
        game.Play(args);
    }
}


public class Game : Logger
{

    public static Map _map;
    public static List<Pellet> _pellets;
    public Dictionary<Pac, string> _moveOutput;

    public void Play(string[] args)
    {
        string[] inputs;
        var inputBoardSize = Console.ReadLine();
        //Err($"inputBoardSize: {inputBoardSize}");
        inputs = inputBoardSize.Split(' ');
        int width = int.Parse(inputs[0]); // size of the grid
        int height = int.Parse(inputs[1]); // top left corner is (x=0, y=0)
        _map = new Map(width, height);
        for (int i = 0; i < height; i++)
        {
            string row = Console.ReadLine(); // one line of the grid: space " " is floor, pound "#" is wall
            _map.PopulateMap(i, row);
            //Err($"inputRow: {row}");
        }
        _pellets = _map.GetAllValidPos().Select(x => new Pellet { Pos = x, Value = 1 }).ToList();
        List<Pac> pacs = new List<Pac>();
        List<Pac> enemyPacs = new List<Pac>();

        // game loop
        while (true)
        {
            #region Read input
            enemyPacs = new List<Pac>();
            var inputScore = Console.ReadLine();
            _moveOutput = new Dictionary<Pac, string>();
            //Err($"inputScore: {inputScore}");
            inputs = inputScore.Split(' ');
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

                if (mine)
                {
                    pacsAlive.Add(pacId);
                    var pac = pacs.Find(p => p.Id == pacId);

                    if (pac == default)
                    {
                        pacs.Add(new Pac(_map)
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
                        pac.LastMove = pac.Pos;
                        pac.Pos = new Pos(x, y);
                        pac.PacType = Enum.Parse<PacType>(typeId);
                        pac.SpeedTurnsLeft = speedTurnsLeft;
                        pac.AbilityCooldown = abilityCooldown;
                        if (pac.Pos == pac.NextTarget) pac.NextTarget = null; // target has been reached
                    }
                }
                else
                {
                    enemyPacs.Add(new Pac(_map)
                    {
                        Id = pacId,
                        Pos = new Pos(x, y),
                        PacType = Enum.Parse<PacType>(typeId),
                        SpeedTurnsLeft = speedTurnsLeft,
                        AbilityCooldown = abilityCooldown
                    });
                }
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
                visiblePellets.Add(new Pellet { Pos = new Pos(x, y), Value = value });
            }
            #endregion read input

            #region STATE UPDATE
            var superPellets = visiblePellets.Where(p => p.IsSuperPellet);
            if (superPellets.Any())
            {
                foreach (var pellet in superPellets)
                {
                    _pellets.Find(p => p.Pos == pellet.Pos).Value = pellet.Value;
                }
            }

            var visibleTiles = GetVisibleTiles(pacs);

            var tilesWithoutPellets = visibleTiles.Select(x => x).ToList();
            foreach (var pellet in visiblePellets)
            {
                tilesWithoutPellets.Remove(pellet.Pos);
            }

            foreach (var pos in tilesWithoutPellets)
            {
                var indexToRemvoe = _pellets.FindIndex(p => p.Pos == pos);
                if (indexToRemvoe >= 0) _pellets.RemoveAt(indexToRemvoe);
            }

            // clear next target if I know the pellet is gone
            foreach (var pac in pacs)
            {
                if (tilesWithoutPellets.Contains(pac.NextTarget)) pac.NextTarget = null;
            }
            #endregion STATE UPDATE

            //calcualte distances to pellets
            foreach (var pellet in _pellets)
            {
                foreach (var pac in pacs)
                {
                    //var aStarPathFinder = new AStarPathFiner();
                    //var path = aStarPathFinder.FindPath(_map, pac.Pos, pellet.Pos);
                    //Err($"path: {path.EnumerableToString()}");
                    //Err($"path count: {path.Count}");

                    var distance = GetDistance(pac.Pos, pellet.Pos);

                    if (pellet.PacDistances.ContainsKey(pac))
                    {
                        pellet.PacDistances[pac] = distance;
                        //pellet.PacDistances[pac] = path.Count();
                    }
                    else
                    {
                        pellet.PacDistances.Add(pac, distance);
                        //pellet.PacDistances.Add(pac, path.Count());
                    }
                }
            }

            //Find next target
            foreach (var pac in pacs)
            {
                //if (pac.HasTarget) continue;

                // aim for enemy
                if (enemyPacs.Any())
                {
                    var enemyPac = enemyPacs.First();
                    pac.NextTarget = enemyPac.Pos;
                    var forcePathRecalc = pac.NextMove;

                    Err($"pac.Path:{pac.Path.EnumerableToString()}");
                    if (pac.Path.Count() >= 4 || (!enemyPac.IsOnSpeed && pac.Path.Count() >= 4))
                    {
                        _moveOutput[pac] = $"MOVE {pac.Id} {GetPathMove(pac)} '{GetPathMove(pac)}'";
                        Err($"ATTACK MODE P{pac.Id}! pac.Path.Count() > 4. moving to enemy");
                        continue;
                    }

                    // I am stronger type
                    if (pac.PacType.IsOpposite(enemyPac.PacType))
                    {
                        if (pac.Path.Count() <= 2 || enemyPac.AbilityCooldown == 0)
                        {

                            if (enemyPac.AbilityCooldown == 0 && 
                                ((pac.Path.Count == 1 && !enemyPac.IsOnSpeed) ||
                                (enemyPac.IsOnSpeed && pac.Path.Count == 2)))
                            {
                                //he is likly to chagne or die so wait to see if he changes
                                _moveOutput[pac] = $"MOVE {pac.Id} {pac.Pos} '{pac.Pos}'";
                                Err($"ATTACK MODE P{pac.Id}! I am stronger, enemy could SWITCH and eat me");
                                continue;
                            }

                            _moveOutput[pac] = $"MOVE {pac.Id} {GetPathMove(pac)} '{GetPathMove(pac)}'";
                            Err($"ATTACK MODE P{pac.Id}! pac.Path.Count() <= 2. I am stronger get him");
                            continue;
                        }
                        else
                        {
                            if (pac.AbilityCooldown == 0)
                            {
                                _moveOutput[pac] = $"SPEED {pac.Id}"; // SPEED <pacId>
                                Err($"ATTACK MODE P{pac.Id}! I am stronger get him but he is far. SPEED");
                                continue;
                            }
                        }
                    }
                    //same type
                    else if(pac.PacType == enemyPac.PacType)
                    {
                        if (pac.AbilityCooldown == 0)
                        {
                            _moveOutput[pac] = $"SWITCH {pac.Id} {enemyPac.PacType.GetOpposite()}";
                            Err($"ATTACK MODE P{pac.Id}! Same Type. Switch to stroner type");
                            continue;
                        }
                        else
                        {
                            _moveOutput[pac] = $"MOVE {pac.Id} {GetPathMove(pac)} '{GetPathMove(pac)}'";
                            Err($"ATTACK MODE P{pac.Id}! Same Type. no ability move to block/closer");
                            continue;
                        }

                    }
                    // I am weaker
                    else if (enemyPac.PacType.IsOpposite(pac.PacType))
                    {
                        if (pac.AbilityCooldown == 0)
                        {
                            if((enemyPac.IsOnSpeed && pac.Path.Count > 3) ||
                                (pac.Path.Count > 2 && !enemyPac.IsOnSpeed))
                            {
                                _moveOutput[pac] = $"MOVE {pac.Id} {GetPathMove(pac)} '{GetPathMove(pac)}'";
                                Err($"ATTACK MODE P{pac.Id}! I am weaker, enemy at 3 or 4s dist. move forward 1");
                                continue;
                            }

                            if ((pac.Path.Count == 2 && !enemyPac.IsOnSpeed) ||
                                (enemyPac.IsOnSpeed && pac.Path.Count == 3)) 
                            {
                                //dont move && bait him
                                _moveOutput[pac] = $"MOVE {pac.Id} {pac.Pos} '{pac.Pos}'";
                                Err($"ATTACK MODE P{pac.Id}! I am weaker, enemy at 2 or 3 dist. bait him");
                                continue;
                            }

                            if ((pac.Path.Count == 1 && !enemyPac.IsOnSpeed) ||
                                (enemyPac.IsOnSpeed && pac.Path.Count == 2))
                            {
                                _moveOutput[pac] = $"SWITCH {pac.Id} {enemyPac.PacType.GetOpposite()}";
                                Err($"ATTACK MODE P{pac.Id}! I am weaker, enemy right on me SWITCH & Kill");
                                continue;
                            }
                        }
                        else
                        {
                            var moves = _map.ValidAdjecentPos(pac.Pos);
                            moves.RemoveAll(m => pac.Path.Contains(m));

                            if (pac.SpeedTurnsLeft != 0)
                            {
                                var doubleMove = new List<Pos>();
                                foreach (var pos in moves)
                                {
                                    doubleMove.AddRange(_map.ValidAdjecentPos(pos));
                                }
                                 doubleMove.AddRange(moves);
                                moves = doubleMove.Distinct().ToList();
                                var edgeMove = new List<Pos>();
                                foreach (var move in moves)
                                {
                                    if ((Math.Abs(move.X - pac.Pos.X) == 2 && move.Y == pac.Pos.Y) ||
                                        (Math.Abs(move.Y - pac.Pos.Y) == 2 && move.X == pac.Pos.X) ||
                                        (Math.Abs(move.X - pac.Pos.X) == 1 && Math.Abs(move.Y - pac.Pos.Y) == 1) ||
                                        (Math.Abs(move.Y - pac.Pos.Y) == 1 && Math.Abs(move.X - pac.Pos.X) == 1))
                                    {
                                        edgeMove.Add(move);
                                    }
                                }
                                edgeMove.RemoveAll(m => pac.Path.Contains(m));
                                _moveOutput[pac] = $"MOVE {pac.Id} {edgeMove.First()} '{edgeMove.First()}'";
                                Err($"ATTACK MODE P{pac.Id}! I am weaker, no ability run with SPEED");
                                continue;
                            }

                            _moveOutput[pac] = $"MOVE {pac.Id} {moves.First()} '{moves.First()}'";
                            Err($"ATTACK MODE P{pac.Id}! I am weaker, no ability run");
                            continue;
                        }
                    }
                }

                // aim for Super pellet
                var superPelletsClosestToThisPac = _pellets?.Where(x => x.IsSuperPellet)?.Where(x => x.ClosestPac.HasValue && x.ClosestPac.Value.Key == pac)?.OrderBy(x => x.ClosestPac.Value.Value);
                var closestSuperPelelt = superPelletsClosestToThisPac?.FirstOrDefault();
                pac.NextTarget = closestSuperPelelt?.Pos;

                // if not super pellet go for closest pellet
                if (!pac.HasTarget)
                {
                    var pelletsClosestToThisPac = _pellets?.Where(x => x.ClosestPac.HasValue && x.ClosestPac.Value.Key == pac)?.OrderBy(x => x.ClosestPac.Value.Value);
                    var closestPelletToThisPac = pelletsClosestToThisPac?.FirstOrDefault();
                    pac.NextTarget = closestPelletToThisPac?.Pos;

                    if (!pac.HasTarget)
                    {
                        pelletsClosestToThisPac = _pellets?.Select(p => new Pellet { Pos = p.Pos, Value = p.Value, PacDistances = p.PacDistances?.Where(x => x.Key == pac)?.ToDictionary(y => y.Key, j => j.Value) })?.OrderBy(p => p.Value);
                        closestPelletToThisPac = pelletsClosestToThisPac?.FirstOrDefault();
                        pac.NextTarget = closestPelletToThisPac?.Pos;
                    }
                }
            }

            ////change target if opposite enemy is infront
            //foreach (var pac in pacs)
            //{
            //    var enemyInfont = enemyPacs.Where(p => p.Pos == pac.NextMove || p.Pos == pac.DoubleForward);
            //    if (enemyInfont.Any())
            //    {
            //        if (enemyInfont.Count() == 1)
            //        {
            //            var enemyPac = enemyInfont.First();
            //            if (enemyPac.PacType.IsOpposite(pac.PacType))
            //            {
            //                // ?? 
            //                Err($"Enemy Infront is STRONG vs ME");
            //                if (pac.AbilityCooldown == 0)
            //                {
            //                    _moveOutput[pac] = $"SWITCH {pac.Id} {enemyPac.PacType.GetOpposite()}";
            //                }
            //                AStarPathFiner pather = new AStarPathFiner();
            //                var path = pather.FindPath(_map, pac.Pos, pac.NextTarget, avoidEnemy: true, enemyPacs);
            //                var newNextMove = path.FirstOrDefault();
            //                if (newNextMove != null)
            //                {
            //                    pac.NextMove = newNextMove;
            //                    _moveOutput[pac] = $"MOVE {pac.Id} {pac.NextMove}";
            //                }
            //                else
            //                {
            //                    pac.NextMove = pac.LastMove;
            //                    _moveOutput[pac] = $"MOVE {pac.Id} {pac.NextMove}";
            //                }

            //            }
            //            else if (pac.PacType.IsOpposite(enemyPac.PacType))
            //            {
            //                Err($"Enemy Infront is WEAK vs ME");

            //                // keep going I win
            //            }
            //            else if(pac.PacType == enemyPac.PacType)
            //            {
            //                Err($"Enemy Infront is SAME TYPE");

            //                // we are the same type. let it block
            //            }
            //        }
            //    }
            //}

            //Adjust target based on speed
            foreach (var pac in pacs)
            {
                if (pac.HasTarget && pac.NextTarget.IsAdjacent(pac.Pos) && pac.IsOnSpeed)
                {
                    var adjecentPoses = _map.ValidAdjecentPos(pac.NextTarget);
                    //Err($"adjecentPoses {pac.Id}: {adjecentPoses.EnumerableToString()}");
                    if (adjecentPoses.Any())
                    {
                        adjecentPoses.Remove(pac.Pos);
                        if (adjecentPoses.Any())
                        {
                            var pelletsPoses = _pellets.Select(x => x.Pos);
                            var adjecentPellets = pelletsPoses.Intersect(adjecentPoses);
                            //Err($"adjecentPellets: {adjecentPellets.EnumerableToString()}");
                            if (adjecentPellets.Any())
                            {
                                pac.NextTarget = adjecentPellets.First();
                            }
                            else
                            {
                                pac.NextTarget = adjecentPoses.First();
                            }
                        }
                    }
                }
            }

            foreach (var pac in pacs)
            {
                //if (pac.AbilityCooldown == 0)
                //{
                //    if (pac.IsBlocked)
                //    {
                //        //Err($"pac.IsBlocked && Ability cool down is 0");
                //        //Err($"enemyPac: {enemyPacs.EnumerableToString()}");
                //        //Err($"my pac: {pac}");
                //        //Err($"My Pos: {pac.Pos}, My Forward: {pac.Forward}, My DoubleForward Pos: {pac.DoubleForward}, DoubleEast: {pac.Pos.East.East}");
                //        var blockingEnemy = enemyPacs.Find(p => p.Pos == pac.Forward || p.Pos == pac.DoubleForward);
                //        //Err($"blockingEnemy: {blockingEnemy}");
                //        if (blockingEnemy != default) // blocked by enemy change to opposite type
                //        {
                //            var oppositeType = blockingEnemy.PacType.GetOpposite();
                //            if (!_moveOutput.ContainsKey(pac)) _moveOutput[pac] = $"SWITCH {pac.Id} {oppositeType}";
                //        }
                //    }
                //    else
                //    {
                //        //if(!_moveOutput.ContainsKey(pac)) _moveOutput[pac] = $"SPEED {pac.Id}"; // SPEED <pacId>
                //    }
                //}
                if (pac.NextTarget != null)
                {
                    var blockingPac = pacs.Find(p => p.Pos == pac.Forward);
                    if (blockingPac != default) // blocked by me
                    {
                        //Err($"Blocked by me. Pac {pac.Id} is blocked by {blockingPac.Id}.");
                        var pacNexttarget = pac.NextTarget;
                        pac.NextTarget = blockingPac.NextTarget;
                        blockingPac.NextTarget = pacNexttarget;
                    }

                    if (!_moveOutput.ContainsKey(pac)) _moveOutput[pac] = $"MOVE {pac.Id} {pac.NextTarget} '{pac.NextTarget}'"; // MOVE <pacId> <x> <y> <message>

                }
            }

            Console.WriteLine(_moveOutput.Values.Aggregate((a, b) => $"{a}|{b}"));
        }
    }

    private static Pos GetPathMove(Pac pac)
    {
        return (pac.SpeedTurnsLeft != 0 && pac.Path.Count > 1) ? pac.Path[1] : pac.Path[0];
    }

    private List<Pos> GetVisibleTiles(List<Pac> pacs)
    {
        var visibleTiles = new List<Pos>();
        foreach (var pac in pacs)
        {
            visibleTiles.Add(pac.Pos); // Add pac location
            //North
            var nextNorthTile = pac.Pos.North;
            while (_map.IsFloorTile(nextNorthTile))
            {
                visibleTiles.Add(nextNorthTile);
                nextNorthTile = nextNorthTile.North;
            }

            // East
            var nextEastTile = pac.Pos.East;
            while (_map.IsFloorTile(nextEastTile))
            {
                visibleTiles.Add(nextEastTile);
                nextEastTile = nextEastTile.East;
            }

            //South
            var nextSouthTile = pac.Pos.South;
            while (_map.IsFloorTile(nextSouthTile))
            {
                visibleTiles.Add(nextSouthTile);
                nextSouthTile = nextSouthTile.South;
            }

            // West
            var nextWestTile = pac.Pos.West;
            while (_map.IsFloorTile(nextWestTile))
            {
                visibleTiles.Add(nextWestTile);
                nextWestTile = nextWestTile.West;
            }
        }

        return visibleTiles.Distinct().ToList();
    }

    private double GetDistance(Pos from, Pos to)
    {
        return Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
    }
}

public class Logger {
    public void Err(string msg)
    {
        Console.Error.WriteLine(msg);
    }
}

public class Pac : Logger, IEquatable<Pac>
{
    private Map _map;

    public Pac(Map map)
    {
        _map = map;
    }

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

public static class ExtensionMethods
{
    public static PacType GetOpposite(this PacType pacType)
    {
        switch (pacType)
        {
            case PacType.ROCK:
                return PacType.PAPER;
            case PacType.PAPER:
                return PacType.SCISSORS;
            case PacType.SCISSORS:
                return PacType.ROCK;
            default:
                throw new Exception();
        }
    }

    public static bool IsOpposite(this PacType type1, PacType type2)
    {
        return type1 == type2.GetOpposite();
    }
}

public class Pellet : IEquatable<Pellet>
{
    public Pos Pos { get; set; }
    public int Value { get; set; }

    public bool IsSuperPellet  => Value == 10;

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

public class Pos : IEquatable<Pos>
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
    public Pos North => new Pos(X , Y - 1);
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

public class Map
{
    public int Width { get; set; }
    public int Height { get; set; }
    public Tile[,] GameMap { get; set; }

    public Map(int width, int height)
    {
        Width = width;
        Height = height;
        GameMap = new Tile[height, width];
    }

    public void PopulateMap(int lineNo, string line)
    {
        for (int i = 0; i < line.Length; i++)
        {
            var tile = (line[i] == ' ') ? Tile.Floor : Tile.Wall;
            GameMap[lineNo, i] = tile;
        }
    }

    public override string ToString()
    {
        StringBuilder result = new StringBuilder();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                result.Append((GameMap[y, x] == Tile.Floor) ? ' ' : '#');
            }
            result.AppendLine();
        }
        return result.ToString();
    }

    internal List<Pos> ValidAdjecentPos(Pos pos)
    {
        var result = new List<Pos>();

        var north = new Pos(pos.X, pos.Y - 1);
        if (IsFloorTile(north)) result.Add(north);

        var east = new Pos(pos.X + 1, pos.Y);
        if (IsFloorTile(east)) result.Add(east);

        var south = new Pos(pos.X, pos.Y + 1);
        if (IsFloorTile(south)) result.Add(south);

        var west = new Pos(pos.X - 1, pos.Y);
        if (IsFloorTile(west)) result.Add(west);

        return result;
    }

    internal IEnumerable<Pos> GetAllValidPos()
    {
        var validTilesPos = new List<Pos>();
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (GameMap[y, x] == Tile.Floor)
                {
                    validTilesPos.Add(new Pos(x, y));
                }
            }
        }
        return validTilesPos;
    }

    internal bool IsPosOnBoard(Pos pos)
    {
        return pos.X >= 0 && pos.Y >= 0 && pos.X < Width && pos.Y < Height;
    }

    internal bool IsFloorTile(Pos pos)
    {
        return IsPosOnBoard(pos) && GameMap[pos.Y, pos.X] == Tile.Floor;
    }
}

public enum Tile
{
    Wall,
    Floor
}

public static class IEnumerableExtension
{
    public static string EnumerableToString<T>(this IEnumerable<T> source)
    {
        if (source == null) return "NullSequence";
        if (!source.Any()) return "EmptySequence";

        return source.Select(x => x.ToString()).Aggregate((a, b) => $"{a}|{b}");
    }
}

public class AStarPathFiner
{
    Dictionary<Pos, bool> _closedSet = new Dictionary<Pos, bool>();
    Dictionary<Pos, bool> _openSet = new Dictionary<Pos, bool>();

    //cost of start to this key node
    Dictionary<Pos, int> _gScore = new Dictionary<Pos, int>();
    //cost of start to goal, passing through key node
    Dictionary<Pos, int> _fScore = new Dictionary<Pos, int>();

    Dictionary<Pos, Pos> _nodeLinks = new Dictionary<Pos, Pos>();

    public List<Pos> FindPath(Map map, Pos start, Pos goal, bool avoidEnemy = false, List<Pac> enemyPacs = null)
    {
        _openSet[start] = true;
        _gScore[start] = 0;
        _fScore[start] = Heuristic(start, goal);
        Stopwatch timer = new Stopwatch();
        while (_openSet.Count > 0)
        {
            if (!timer.IsRunning)
            {
                timer.Start();
            }
            //Console.Error.WriteLine($"Duration {timer.ElapsedMilliseconds}ms.");
            //Console.Error.WriteLine($"FindPath: {_openSet.Count}");
            var current = NextBest();
            if (current.Equals(goal))
            {
                return Reconstruct(current);
            }

            _openSet.Remove(current);
            _closedSet[current] = true;

            var neighbors = Neighbors(map, current, avoidEnemy, enemyPacs);
            //Console.Error.WriteLine($"FindPath neighbors: {neighbors.Select(x => x.ToString()).Aggregate((a,b)=> a + " | " + b )}");

            foreach (var neighbor in neighbors)
            {

                if (_closedSet.ContainsKey(neighbor))
                    continue;

                var projectedG = GetGScore(current) + 1;

                if (!_openSet.ContainsKey(neighbor))
                    _openSet[neighbor] = true;
                else if (projectedG >= GetGScore(neighbor))
                    continue;

                //record it
                _nodeLinks[neighbor] = current;
                _gScore[neighbor] = projectedG;
                _fScore[neighbor] = projectedG + Heuristic(neighbor, goal);

            }
        }

        return new List<Pos>();
    }

    private int Heuristic(Pos start, Pos goal)
    {
        var dx = goal.X - start.X;
        var dy = goal.Y - start.Y;
        return Math.Abs(dx) + Math.Abs(dy);
    }

    private int GetGScore(Pos pt)
    {
        int score = int.MaxValue;
        _gScore.TryGetValue(pt, out score);
        return score;
    }


    private int GetFScore(Pos pt)
    {
        int score = int.MaxValue;
        _fScore.TryGetValue(pt, out score);
        return score;
    }

    public static IEnumerable<Pos> Neighbors(Map map, Pos center, bool avoidEnemy, List<Pac> enemyPacs)
    {
        //North
        Pos pt = center.North;
        if (IsValidNeighbor(map, pt, avoidEnemy, enemyPacs)) 
            yield return pt;
        //East
        pt = center.East;
        if (IsValidNeighbor(map, pt, avoidEnemy, enemyPacs))
            yield return pt;
        //South
        pt = center.South;
        if (IsValidNeighbor(map, pt, avoidEnemy, enemyPacs))
            yield return pt;
        //West
        pt = center.West;
        if (IsValidNeighbor(map, pt, avoidEnemy, enemyPacs))
            yield return pt;
    }

    private static bool IsEnemy(List<Pac> enemyPacs, Pos pt)
    {
        return enemyPacs?.Where(e => e.Pos == pt)?.Any() ?? false;
    }

    public static bool IsValidNeighbor(Map map, Pos pt, bool avoidEnemy, List<Pac> enemyPacs)
    {
        if (map.IsPosOnBoard(pt) && map.IsFloorTile(pt))
        {
            if (avoidEnemy)
            {
                return !IsEnemy(enemyPacs, pt);
            }

            return true;

        }
        return false;
    }

    private List<Pos> Reconstruct(Pos current)
    {
        List<Pos> path = new List<Pos>();
        while (_nodeLinks.ContainsKey(current))
        {
            path.Add(current);
            current = _nodeLinks[current];
        }

        path.Reverse();
        return path;
    }

    private Pos NextBest()
    {
        int best = int.MaxValue;
        Pos bestPt = null;
        foreach (var node in _openSet.Keys)
        {
            var score = GetFScore(node);
            if (score < best)
            {
                bestPt = node;
                best = score;
            }
        }

        return bestPt;
    }
}