using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Grab the pellets as fast as you can!
 **/
class Player
{
    static void Main(string[] args)
    {
        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        int width = int.Parse(inputs[0]); // size of the grid
        int height = int.Parse(inputs[1]); // top left corner is (x=0, y=0)
        for (int i = 0; i < height; i++)
        {
            string row = Console.ReadLine(); // one line of the grid: space " " is floor, pound "#" is wall
        }
        List<int> pacs = new List<int>();
        List<Pac> pacs2 = new List<Pac>();

        // game loop
        while (true)
        {
            pacs = new List<int>();
            pacs2 = new List<Pac>();
            inputs = Console.ReadLine().Split(' ');
            int myScore = int.Parse(inputs[0]);
            int opponentScore = int.Parse(inputs[1]);
            int visiblePacCount = int.Parse(Console.ReadLine()); // all your pacs and enemy pacs in sight
            for (int i = 0; i < visiblePacCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                var pacId = int.Parse(inputs[0]);
                bool mine = inputs[1] != "0"; // true if this pac is yours
                if(mine) pacs.Add(pacId); // pac number (unique within a team)
                int x = int.Parse(inputs[2]); // position in the grid
                int y = int.Parse(inputs[3]); // position in the grid
                string typeId = inputs[4]; // unused in wood leagues
                int speedTurnsLeft = int.Parse(inputs[5]); // unused in wood leagues
                int abilityCooldown = int.Parse(inputs[6]); // unused in wood leagues

                if (mine)
                {
                    pacs2.Add(new Pac { Id = pacId, Pos = new Pos(x, y) });
                }
            }
            int visiblePelletCount = int.Parse(Console.ReadLine()); // all pellets in sight
            Dictionary<Pos, int> pellets = new Dictionary<Pos, int>();
            List<Pellet> pellets2 = new List<Pellet>();
            for (int i = 0; i < visiblePelletCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int x = int.Parse(inputs[0]);
                int y = int.Parse(inputs[1]);
                int value = int.Parse(inputs[2]); // amount of points this pellet is worth
                pellets.Add(new Pos(x, y), value);
                pellets2.Add(new Pellet { Pos = new Pos(x, y), Value = value });
            }

            Dictionary<Pac, Dictionary<Pellet, double>> pacDistanceToPellets = new Dictionary<Pac, Dictionary<Pellet, double>>();
            foreach (var pac in pacs2)
            {
                Dictionary<Pellet, double> distanceToPellet = new Dictionary<Pellet, double>();
                foreach (var pellet in pellets2)
                {
                    var distance = GetDistance(pac.Pos, pellet.Pos);
                    distanceToPellet.Add(pellet, distance);
                }
                distanceToPellet.OrderBy(x => x.Value);
                pac.NextTarget = distanceToPellet.FirstOrDefault().Key.Pos;
                pacDistanceToPellets.Add(pac, distanceToPellet);
            }

            foreach (var pac in pacDistanceToPellets)
            {
                var pacsNextTarget = pacDistanceToPellets.Where(x => x.Key != pac.Key).Select(x => x.Key.NextTarget);
                if (pacsNextTarget.Contains(pac.Key.NextTarget))
                {
                     
                }
            }

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");
            var moveOutput = "";
            foreach (var pac in pacs)
            {
                var firstHighestValPellet = pellets.OrderByDescending(x => x.Value)?.FirstOrDefault();
                if (firstHighestValPellet != null && firstHighestValPellet.HasValue && firstHighestValPellet.Value.Key != null)
                {
                    pellets.Remove(firstHighestValPellet.Value.Key);
                    moveOutput += $"MOVE {pac} {firstHighestValPellet.Value.Key} YAY"; // MOVE <pacId> <x> <y>
                    if (pacs.Last() != pac)
                    {
                        moveOutput += "|";
                    }
                }
            }

            Console.WriteLine(moveOutput); 

        }
    }

    private static double GetDistance(Pos from, Pos to)
    {
        return Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
    }
}

public class Pac
{
    public int Id { get; set; }
    public Pos Pos { get; set; }
    public Pos NextTarget { get; set; }

}

public class Pellet
{
    public Pos Pos { get; set; }
    public int Value { get; set; }
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

    public override string ToString()
    {
        return $"{X} {Y}";
    }

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
}