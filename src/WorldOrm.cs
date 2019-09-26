using System;

namespace Starcounter.Techempower
{
    public class WorldOrm : IWorld
    {
        public WorldOrm() { }
        public WorldOrm(IWorld w)
        {
            Id = w.Id;
            RandomNumber = w.RandomNumber;
        }
        public int Id { get; set; }
        public int RandomNumber { get; set; }
    }
}
