using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostCell
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class Player
    {
        public const string FACTORY = "FACTORY";
        public const string TROOP = "TROOP";
        public const string BOMB = "BOMB";

        static void Main(string[] args)
        {
            GM gm = new GM();

            string[] inputs;
            int factoryCount = int.Parse(Console.ReadLine()); // the number of factories
            int linkCount = int.Parse(Console.ReadLine()); // the number of links between factories
            for (int i = 0; i < linkCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int factory1 = int.Parse(inputs[0]);
                int factory2 = int.Parse(inputs[1]);
                int distance = int.Parse(inputs[2]);
                gm.Add(new Link(factory1, factory2, distance));
                Console.Error.WriteLine($"{factory1} {factory2} - {distance}");
            }

            // game loop
            while (true)
            {
                int entityCount = int.Parse(Console.ReadLine()); // the number of entities (e.g. factories and troops)
                for (int i = 0; i < entityCount; i++)
                {
                    Entity e = new Entity();
                    inputs = Console.ReadLine().Split(' ');
                    e.Id = int.Parse(inputs[0]);
                    e.Type = inputs[1];
                    e.Arg1 = int.Parse(inputs[2]);
                    e.Arg2 = int.Parse(inputs[3]);
                    e.Arg3 = int.Parse(inputs[4]);
                    e.Arg4 = int.Parse(inputs[5]);
                    e.Arg5 = int.Parse(inputs[6]);
                    gm.Add(e);
                }
                Console.WriteLine(gm.Output());
            }
        }

        public class GM
        {
            List<Entity> factories = new List<Entity>();
            List<Entity> troops = new List<Entity>();
            List<Entity> bombs = new List<Entity>();
            Graph graph = new Graph();
            int bombCount = 2;
            bool firstTurn = true;

            public string Output()
            {
                List<string> command = new List<string>();
                UpdateW();
                if (firstTurn)
                {
                    Concuer(command);
                    firstTurn = false;
                }
                else
                {
                    Battle(command);
                }

                troops = new List<Entity>();
                bombs = new List<Entity>();

                if (command.Count == 0)
                    return "WAIT";
                else
                    return string.Join(";", command);
            }

            public void Concuer(List<string> command)
            {
                Entity mf = factories.FirstOrDefault(x => x.Arg1 == 1);
                Entity ef = factories.FirstOrDefault(x => x.Arg1 == -1);
                List<Entity> netral = factories
                    .Where(x => x.Arg1 == 0 && x.Arg3 > 0)
                    .Where(x => graph.Distance(x, mf) <= graph.Distance(x, ef))
                    .OrderBy(x => graph.Distance(x, mf))
                    .ToList();
                for (int i = 0; i < netral.Count; i++)
                {
                    int c = netral[i].Arg2 < mf.Arg2 ? netral[i].Arg2 + 1 : -1;
                    mf.Arg2 -= c;
                    if (c != -1)
                        command.Add($"MOVE {mf.Id} {netral[i].Id} {c}");
                }

            }

            public void Battle(List<string> command)
            {
                Entity e = factories
                    .Where(x => x.W < 0)
                    .OrderByDescending(x => x.W)
                    .FirstOrDefault();
                if (e == null)
                    e = factories
                        .OrderBy(x => x.W)
                        .FirstOrDefault();
                List<Entity> fs = factories
                    .Where(x => x.W > 5 && x.Arg1 == 1)
                    .OrderBy(x => graph.Distance(x, e))
                    .ToList();

                if (fs.Count > 1 && bombCount > 0 && !bombs.Any(x => x.Arg1 == 1) && e.Arg1 != 1)
                {
                    command.Add($"BOMB {fs[0].Id} {e.Id}");
                    bombCount--;
                }

                for (int i = 0; i < fs.Count; i++)
                {
                    if (!troops.Any(x => x.Arg1 == -1 && x.Arg3 == fs[i].Id) && fs[i].Arg2 >= 10 && fs[i].Arg3 < 3)
                    {
                        command.Add($"INC {fs[i].Id}");
                    }
                    else
                    {
                        int d = (int)fs[i].W;// > 1 ? fs[i].Arg2 / 2 : fs[i].Arg2;
                        command.Add($"MOVE {fs[i].Id} {graph.Step(fs[i], e)} {d}");
                    }
                }
            }
            
            public void UpdateW()
            {
                List<Entity> fc = factories;

                for (int i = 0; i < fc.Count; i++)
                {
                    int a = fc[i].Arg1 == 1 ? 1 : -1;
                    double e = troops
                        .Where(x => x.Arg1 == -1 && x.Arg3 == fc[i].Id)
                        .Sum(x =>
                        {
                            double r = x.Arg5 == 0 ? x.Arg4 : (double)x.Arg4 / (double)x.Arg5; //x.Arg4 - x.Arg5 * fc[i].Arg3;
                            return r > 0 ? r : 0;
                        });
                    double m = troops
                        .Where(x => x.Arg1 == 1 && x.Arg3 == fc[i].Id)
                        .Sum(x =>
                        {
                            double r = x.Arg5 == 0 ? x.Arg4 : (double)x.Arg4 / (double)x.Arg5; //x.Arg4 - x.Arg5 * fc[i].Arg3;
                            return r > 0 ? r : 0;
                        });
                    fc[i].W = a * fc[i].Arg2 - e + m - fc[i].Arg4 * 100;
                    Console.Error.WriteLine($"{fc[i].Id} : {fc[i].W:0.00}");
                }
            }

            public Entity GetNearF(Entity e, List<Entity> list)
            {
                int min = int.MaxValue;
                Entity near = null;
                for (int i = 0; i < list.Count; i++)
                {
                    int l = graph.Distance(e.Id, list[i].Id);
                    if (min > l)
                    {
                        min = l;
                        near = list[i];
                    }
                }
                return near;
            }

            public void Add(Entity e)
            {
                switch (e.Type)
                {
                    case FACTORY: UpdateFactory(e); break;
                    case TROOP: troops.Add(e); break;
                    case BOMB: bombs.Add(e); break;
                }
            }

            public void Add(Link l)
            {
                graph.Add(l);
            }

            public void UpdateFactory(Entity factory)
            {
                Entity f = factories.FirstOrDefault(x => x.Equals(factory));
                if (f != null)
                {
                    f.Update(factory);
                }
                else
                {
                    factories.Add(factory);
                    graph.SetE(factory);
                }
            }
        }

        #region objects
        public class Entity : IComparable
        {
            public int Id { get; set; }
            public string Type { get; set; }
            public int Arg1 { get; set; }
            public int Arg2 { get; set; }
            public int Arg3 { get; set; }
            public int Arg4 { get; set; }
            public int Arg5 { get; set; }
            public double W { get; set; }
            public override bool Equals(object obj)
            {
                Entity e = obj as Entity;
                if (e != null)
                    return this.Id == e.Id;
                else
                    return false;
            }
            public override int GetHashCode()
            {
                return this.Id;
            }
            public override string ToString()
            {
                return $"{this.Type} {this.Id} : {Arg1} {Arg2} {Arg3} {Arg4} {Arg5}";
            }
            public int CompareTo(object obj)
            {
                return W.CompareTo(obj);
            }
            public void Update(Entity newEntity)
            {
                this.Arg1 = newEntity.Arg1;
                this.Arg2 = newEntity.Arg2;
                this.Arg3 = newEntity.Arg3;
                this.Arg4 = newEntity.Arg4;
                this.Arg5 = newEntity.Arg5;
            }
        }

        public class Link
        {
            public int Factory1 { get; set; }
            public int Factory2 { get; set; }
            public Entity F1 { get; set; }
            public Entity F2 { get; set; }
            public int Distance { get; set; }
            public Link()
            {

            }
            public Link(int f1, int f2, int d)
            {
                this.Factory1 = f1;
                this.Factory2 = f2;
                this.Distance = d;
            }
            public void SetFactory(Entity e)
            {
                if (this.Factory1 == e.Id)
                    this.F1 = e;
                else
                    this.F2 = e;
            }
            public bool Contains(Entity e)
            {
                return e.Id == Factory1 || e.Id == Factory2;
            }
        }

        public class Graph
        {
            private List<Link> _links;
            public Graph()
            {
                this._links = new List<Link>();
            }
            public void Add(Link l)
            {
                this._links.Add(l);
            }
            public void Add(int f1, int f2, int d)
            {
                this._links.Add(new Link(f1, f2, d));
            }
            public void SetE(Entity e)
            {
                List<Link> links = _links.Where(x => x.Contains(e)).ToList();
                for (int i = 0; i < links.Count; i++)
                    links[i].SetFactory(e);
            }
            public int Distance(int f1, int f2)
            {
                return this._links.Where(x => x.Factory1 == f1 || x.Factory2 == f1)
                    .Where(x => x.Factory1 == f2 || x.Factory2 == f2)
                    .First().Distance;
            }
            public int Distance(Entity f1, Entity f2)
            {
                return this.Distance(f1.Id, f2.Id);
            }
            public int Step(Entity from, Entity to)
            {
                int dist = Distance(from, to);
                List<Link> flinks = _links
                    .Where(x => x.Contains(from))
                    .Where(x => x.Distance <= dist)
                    .ToList();
                List<Link> tlinks = _links
                    .Where(x => x.Contains(to))
                    .Where(x => x.Distance <= dist)
                    .ToList();
                List<Link> optimal = flinks
                    .Where(x => tlinks.Any(y => y.Contains(x.F1) || y.Contains(x.F2)))
                    .Where(x =>
                    {
                        Entity cur = x.F1.Equals(from) ? x.F2 : x.F1;
                        int d = Distance(cur, to) + x.Distance;
                        return d <= dist;
                    })
                    .OrderBy(x => x.Distance)
                    .ToList();
                Link l = optimal.FirstOrDefault();
                if (l == null)
                    return to.Id;
                Entity e = l.F1.Equals(from) ? l.F2 : l.F1;
                return e.Id;
            }
        }
        #endregion
    }
}
