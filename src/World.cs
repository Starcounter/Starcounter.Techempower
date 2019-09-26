using System;
using Starcounter.Nova;
using Starcounter.Nova.Hosting;
using System.Diagnostics;

namespace Starcounter.Techempower
{
    [Database]
    public abstract class World : IWorld
    {
        public const int Count = 10000;

        public static void Populate(ITransactor transactor)
        {
            Random rnd = new Random();

            transactor.Transact(db =>
            {
                int found = 0;

                foreach (World w in db.Sql<World>("SELECT w FROM Starcounter.Techempower.World w"))
                {
                    Debug.Assert(w.Id >= 1);
                    Debug.Assert(w.Id <= Count);
                    Debug.Assert(db.GetOid(w) != 0);

                    found++;
                }

                for (int id = found + 1; id <= Count; id++)
                {
                    var w = db.Insert<World>();
                    w.Id = id;
                    w.RandomNumber = rnd.Next(1, Count + 1);

                    Debug.Assert(w.Id >= 1);
                    Debug.Assert(w.Id <= Count);
                    Debug.Assert(db.GetOid(w) != 0);
                }
            });
        }

        [Index]
        public abstract int Id { get; set; }
        public abstract int RandomNumber { get; set; }
    }
}
