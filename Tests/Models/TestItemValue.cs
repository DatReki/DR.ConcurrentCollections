using Bogus;
using Tests.Core;

namespace Tests.Models
{
    public class TestItemValue
    {
        private static Length TextLength { get; set; } = new(10, 200);
        private static Length RandomLength { get; set; } = new(10, 200);
        private static Length NumberRange { get; set; } = new(10, 1000);

        public TestItemValue()
        {
            Faker faker = new();
            Guid = Generate.RandomGuid(faker);
            Text = Generate.RandomText(TextLength.Min, TextLength.Max, faker);
            Random = Generate.RandomString(RandomLength.Min, RandomLength.Max, faker);
            Number = Generate.RandomNumber(NumberRange.Min, NumberRange.Max, faker: faker);
        }

        public TestItemValue(Faker faker)
        {
            Guid = Generate.RandomGuid(faker);
            Text = Generate.RandomText(TextLength.Min, TextLength.Max, faker);
            Random = Generate.RandomString(RandomLength.Min, RandomLength.Max, faker);
            Number = Generate.RandomNumber(NumberRange.Min, NumberRange.Max, faker: faker);
        }

        public Guid Guid { get; set; }
        public string Text { get; set; }
        public string Random { get; set; }
        public int Number { get; set; }
    }
}
