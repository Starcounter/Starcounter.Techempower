using System;

namespace Starcounter.Techempower
{
    public class FortuneOrm : IFortune
    {
        public FortuneOrm() { }
        public FortuneOrm(IFortune f)
        {
            Id = f.Id;
            Message = f.Message;
        }

        public int Id { get; set; }
        public string Message { get; set; }

        public int CompareTo(object obj)
        {
            IFortune other = obj as IFortune;

            if (other == null)
            {
                throw new ArgumentException($"Object to compare must be of type {nameof(IFortune)}", nameof(obj));
            }

            return CompareTo(other);
        }

        public int CompareTo(IFortune other)
        {
            return Message.CompareTo(other.Message);
        }
    }
}
