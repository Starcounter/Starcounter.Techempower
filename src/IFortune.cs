using System;

namespace Starcounter.Techempower
{
    public interface IFortune : IComparable<IFortune>
    {
        int Id { get; set; }
        string Message { get; set; }
    }
}
