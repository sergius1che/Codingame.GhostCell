using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostCell
{
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

                // Write an action using Console.WriteLine()
                // To debug: Console.Error.WriteLine("Debug messages...");

                Console.WriteLine(gm.Output());


                // Any valid action, such as "WAIT" or "MOVE source destination cyborgs"
                //Console.WriteLine("WAIT");
            }
        }

        public class GM
        {
            List<Entity> factories = new List<Entity>();
            List<Entity> troops = new List<Entity>();
            List<Entity> bombs = new List<Entity>();
            Graph graph = new Graph();
            int bombCount = 2;

            public string Output()
            {
                UpdateW();
                List<Entity> myfct = factories.Where(x => x.Arg1 == 1).ToList();
                List<Entity> nfct = factories
                    .Where(x => x.Arg1 == 0 && x.Arg3 > 0 && myfct.Count < 3 && x.W < 1).ToList();
                List<Entity> efct = factories.Where(x => x.Arg1 == -1 && x.Arg3 > 0 && x.W < 0).ToList();
                List<string> command = new List<string>();

                for (int i = 0; i < myfct.Count; i++)
                {
                    int d = myfct[i].Arg2;// / 2;
                    Entity nf = GetNearF(myfct[i], nfct);
                    Entity ef = GetNearF(myfct[i], efct.Where(x => x.Id != myfct[i].Id).ToList());

                    if (myfct[i].W < 10 || myfct[i].Arg2 < 2)
                    {
                        var f = factories
                            .Where(x => x.Arg1 == -1 && x.Id != myfct[i].Id && x.Arg3 > 0 && x.Arg4 == 0).ToList();
                        if (bombCount > 0 && bombs.Where(x => x.Arg1 == 1).Count() == 0 && f.Count > 0)
                        {
                            command.Add($"BOMB {myfct[i].Id} {Min(f, x => x.W).Id}");
                            bombCount--;
                            Entity b = new Entity();
                            b.Arg1 = 1;
                            bombs.Add(b);
                        }
                    }
                    else if (myfct[i].Arg2 > 10 && myfct[i].Arg3 < 3 && myfct[i].W > 10 && myfct.Count > 3)
                    {
                        command.Add($"INC {myfct[i].Id}");
                        myfct[i].W -= 10;
                    }
                    else if (nf != null)
                    {
                        d = nf.Arg2 < d ? nf.Arg2 + 1 : d;
                        command.Add($"MOVE {myfct[i].Id} {nf.Id} {d}");
                        myfct[i].W -= d;
                        nf.W += d;
                    }
                    else if (ef != null)
                    {
                        d = ef.Arg2 < d ? (ef.Arg2 + myfct[i].Arg2) >> 1 : d;
                        command.Add($"MOVE {myfct[i].Id} {ef.Id} {d}");
                        myfct[i].W -= d;
                        ef.W += d;
                    }

                }

                troops = new List<Entity>();
                bombs = new List<Entity>();

                if (command.Count > 0)
                    return string.Join(";", command);
                else
                    return "WAIT";
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
                            double r = x.Arg4 - x.Arg5 * fc[i].Arg3;
                            return r > 0 ? r : 0;
                        });
                    double m = troops
                        .Where(x => x.Arg1 == 1 && x.Arg3 == fc[i].Id)
                        .Sum(x =>
                        {
                            double r = x.Arg4 - x.Arg5 * fc[i].Arg3;
                            return r > 0 ? r : 0;
                        });
                    fc[i].W = a * fc[i].Arg2 - e + m - fc[i].Arg4 * 100;
                    Console.Error.WriteLine($"{fc[i].Id} : {fc[i].W}");
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

            public Entity Min<T>(List<Entity> list, Func<Entity, T> prop) where T : IComparable
            {
                T min = list.Count > 0 ? prop(list[0]) : default(T);
                Entity e = list.Count > 0 ? list[0] : null;
                for (int i = 0; i < list.Count; i++)
                {
                    T c = prop(list[i]);
                    if (min.CompareTo(c) > 0)
                    {
                        min = c;
                        e = list[i];
                    }
                }
                return e;
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
                    factories.Add(factory);
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
        }
        #endregion
    }
}
