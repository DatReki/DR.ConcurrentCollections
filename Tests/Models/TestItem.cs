using Bogus;
using System.Diagnostics;
using Tests.Core;

namespace Tests.Models
{
    public class TestItem
    {
        public TestItem() { }

        public TestItem(Faker faker) 
        {
            Id = GetId(faker);
            Value = new TestItemValue(faker);
        }

        public TestItem(string id, TestItemValue value)
        {
            Id = id;
            Value = value;
        }

        public string Id { get; set; } = string.Empty;
        public TestItemValue Value { get; set; } = new TestItemValue();

        public static string GetId(Faker faker)
            => $"{Stopwatch.GetTimestamp()}-{Guid.NewGuid()}-{Generate.RandomString(5, 10, faker)}";
    }
}
