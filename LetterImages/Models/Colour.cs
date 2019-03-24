namespace LetterImages.Models
{
    public readonly struct Colour
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public Colour(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }
    }
}