using System;
using Starcounter.Nova;
using Starcounter.Nova.Hosting;
using System.Diagnostics;

namespace Starcounter.Techempower
{
    [Database]
    public abstract class Fortune : IFortune
    {
        public const int Count = 12;
        static private readonly string[] _initial = new string[Count]
        {
            "fortune: No such file or directory",
            "A computer scientist is someone who fixes things that aren't broken.",
            "After enough decimal places, nobody gives a damn.",
            "A bad random number generator: 1, 1, 1, 1, 1, 4.33e+67, 1, 1, 1",
            "A computer program does what you tell it to do, not what you want it to do.",
            "Emacs is a nice operating system, but I prefer UNIX. — Tom Christaensen",
            "Any program that runs right is obsolete.",
            "A list is only as strong as its weakest link. — Donald Knuth",
            "Feature: A bug with seniority.",
            "Computers make very fast, very accurate mistakes.",
            "<script>alert(\"This should not be displayed in a browser alert box.\");</script>",
            "フレームワークのベンチマーク"
        };

        public static void Populate(ITransactor transactor)
        {
            transactor.Transact(db =>
            {
                int found = 0;

                foreach (Fortune f in db.Sql<Fortune>("SELECT f FROM Starcounter.Techempower.Fortune f"))
                {
                    Debug.Assert(f.Id >= 1);
                    Debug.Assert(f.Id <= Count);
                    Debug.Assert(db.GetOid(f) != 0);

                    found++;
                }

                for (int id = found + 1; id <= Count; id++)
                {
                    Fortune f = db.Insert<Fortune>();
                    f.Id = id;
                    f.Message = _initial[id - 1];

                    Debug.Assert(f.Id >= 1);
                    Debug.Assert(f.Id <= Count);
                    Debug.Assert(db.GetOid(f) != 0);
                }
            });
        }

        [Index]
        public abstract int Id { get; set; }
        public abstract string Message { get; set; }

        public int CompareTo(IFortune other)
        {
            return Message.CompareTo(other.Message);
        }
    }
}
