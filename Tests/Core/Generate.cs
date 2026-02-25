using Bogus;
using System.Collections.Concurrent;
using Tests.Models;

namespace Tests.Core
{
    internal class Generate
    {
        private static readonly ParallelOptions _parallelOptions = new() 
        { 
            MaxDegreeOfParallelism = Environment.ProcessorCount < 4 ? Environment.ProcessorCount : 4
        };

        internal static List<TestItem> TestItems(int? count = null, Faker? faker = null)
        {
            faker ??= new Faker();
            count ??= Main.TestOptions.Total * 20;
            ConcurrentBag<TestItem> result = [];

            Parallel.For(0, (int)count, _parallelOptions, i =>
            {
                result.Add(new TestItem(faker));
            });

            return [.. result];
        }

        internal static int RandomNumber(int? min = null, int? max = null, IEnumerable<int>? exclude = null, Faker? faker = null)
        {
            min ??= int.MinValue;
            max ??= int.MaxValue;

            faker ??= new Faker();
            if (exclude != null && exclude.Any())
            {
                List<int> numbers = [.. Enumerable.Range((int)min, Math.Abs((int)max - (int)min) + 1)];
                numbers.RemoveAll(exclude.Contains);

                return numbers[faker.Random.Number(0, numbers.Count - 1)];
            }

            return faker.Random.Number((int)min, (int)max);
        }

        internal static string RandomString(int? min = null, int? max = null, Faker? faker = null)
        {
            faker ??= new Faker();
            return faker.Random.String(RandomNumber(min, max));
        }

        internal static string RandomText(int? min = null, int? max = null, Faker? faker = null)
        {
            faker ??= new Faker();
            return faker.Lorem.Paragraphs(RandomNumber(min, max));
        }

        internal static Guid RandomGuid(Faker? faker = null)
        {
            faker ??= new Faker();
            return faker.Random.Guid();
        }
    }
}
