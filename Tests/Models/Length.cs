namespace Tests.Models
{
    public class Length
    {
        public Length() { }

        public Length(int min, int max) 
        {
            Min = min;
            Max = max;
        }

        public int Min { get; set; } = 0;
        public int Max { get; set; } = 100;
    }
}
