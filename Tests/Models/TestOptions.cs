namespace Tests.Models
{
    public class TestOptions
    {
        public TestOptions() { }

        public TestOptions(int count) 
        {
            if (count % 4 == 0)
            {
                MaxDegreeOfParallelism = 4;
                CountPerAction = count / 4;
            }
            else if (count % 2 == 0)
            {
                MaxDegreeOfParallelism = 2;
                CountPerAction = count / 2;
            }
            else
            {
                MaxDegreeOfParallelism = 1;
                CountPerAction = count;
            }
        }

        public int MaxDegreeOfParallelism { get; set; } = 4;
        public int CountPerAction { get; set; } = 250;
        public int Total 
        {
            get => CountPerAction * MaxDegreeOfParallelism;
        }
    }
}
