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

            public string Output()
            {
                int maxD = factories.Where(x => x.Arg1 == 1).Max(x => x.Arg2);
                maxD = maxD > 10 ? maxD : -1;
                Entity myF = factories.FirstOrDefault(x => x.Arg1 == 1 && x.Arg2 == maxD);
                int d = myF != null ? myF.Arg2 / 2 : -1;
                Entity nF = factories.FirstOrDefault(x => x.Arg1 == 0 && x.Arg2 <= d && x.Arg3 > 0);
                Entity eF = factories.FirstOrDefault(x => x.Arg1 == -1 && x.Arg2 <= d);
                if (myF != null && nF != null)
                    return $"MOVE {myF.Id} {nF.Id} {d}";
                else if (myF != null && eF != null)
                    return $"MOVE {myF.Id} {eF.Id} {d}";
                else
                    return "WAIT";
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
