using Bogus;
using Tests.Models;

namespace Tests
{
    internal class Main
    {
        internal static readonly Faker Faker = new();
        internal static readonly TestOptions TestOptions = new();
        internal static readonly ParallelOptions ParallelOptions = new() 
        {
            MaxDegreeOfParallelism = TestOptions.MaxDegreeOfParallelism,
        };

        [OneTimeSetUp]
        public async Task Setup()
        {
            await ConcurrentObservableCollection.FillReserve(TestOptions.Total);
        }

        [OneTimeTearDown]
        public async Task TearDown() 
        {
            await ConcurrentObservableCollection.Slim.WaitAsync();
            ConcurrentObservableCollection.Slim.Dispose();
        }
    }
}
