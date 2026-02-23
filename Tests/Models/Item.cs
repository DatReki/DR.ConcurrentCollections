using Bogus;
using System.Diagnostics;
using Tests.Core;

namespace Tests.Models
{
    public class Item
    {
        public Item() { }

        public Item(Faker faker) 
        {
            Id = GetId(faker);
            Value = new ItemValue(faker);
        }

        public Item(string id, ItemValue value)
        {
            Id = id;
            Value = value;
        }

        public string Id { get; set; } = string.Empty;
        public ItemValue Value { get; set; } = new ItemValue();

        public static string GetId(Faker faker)
            => $"{Stopwatch.GetTimestamp()}-{Guid.NewGuid()}-{Generate.RandomString(5, 10, faker)}";
    }
}
