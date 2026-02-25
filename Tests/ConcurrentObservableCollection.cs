using Bogus;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using Tests.Core;
using Tests.Models;

namespace Tests
{
    [TestFixture]
    public class ConcurrentObservableCollection
    {
        private Faker Faker;
        private TestOptions TestOptions;
        private ParallelOptions ParallelOptions;
        private static List<EventListenerData> EventCalls = [];
        private static DR.ConcurrentCollections.ConcurrentObservableCollection<TestItem> Collection = [];
        private static readonly SynchronizedCollection<TestItem> Reserve = [];

#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        internal static readonly SemaphoreSlim Slim = new(1);
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method

        [SetUp]
        public async Task Configure()
        {
            Collection = [];
            EventCalls = [];
            Faker = Main.Faker;
            TestOptions = Main.TestOptions;
            ParallelOptions = Main.ParallelOptions;
        }

        [Test]
        public async Task Get()
        {
            await FillCollection();
            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");

            int index = Generate.RandomNumber(0, Collection.Count - 1, faker: Faker);
            TestItem item = Collection[index];

            Assert.That(item, Is.EqualTo(Collection.ElementAt(index)), "Item is different");
        }

        [Test]
        public async Task GetRange()
        {
            await FillCollection();
            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");

            List<TestItem> copy = Collection.GetRange(0, Collection.Count);
            if (copy.Count != Collection.Count)
                Assert.Fail("Collection does not have the expected length");

            foreach (var check in copy)
            {
                if (!Collection.Contains(check))
                    Assert.Fail("Collection does not contain element");
            }

            int index = Generate.RandomNumber(1, Collection.Count - 100, faker: Faker);
            int count = Generate.RandomNumber(5, Collection.Count - index, faker: Faker);
            List<TestItem> range = Collection.GetRange(index, count);

            if (range.Count != count)
                Assert.Fail("Range does not have the expected length");

            for (int i = 0; i < range.Count; i++)
            {
                if (range[i] != Collection[index + i])
                    Assert.Fail("Range does contain expected element");
            }

            Assert.Pass();
        }

        [Test]
        public async Task Add()
        {
            Collection.Clear();
            if (Collection.Count != 0)
                Assert.Fail("Could not clear collection");

            await FillCollection();
            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");

            Assert.That(TestOptions.Total, Is.EqualTo(Collection.Count), "The collection has an unexpected count");
        }

        [Test]
        public async Task AddParallel()
        {
            Collection.Clear();
            if (Collection.Count != 0)
                Assert.Fail("Could not clear collection");

            await FillCollection();
            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");

            Assert.That(TestOptions.Total, Is.EqualTo(Collection.Count), "The collection has an unexpected count");
        }

        [Test]
        public async Task AddRange()
        {
            Collection.Clear();
            if (Collection.Count != 0)
                Assert.Fail("Could not clear collection");

            List<TestItem> items = await GetItems(TestOptions.Total);
            Collection.AddRange(items);

            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");
            else if (Collection.Count != items.Count)
                Assert.Fail("Collection has an unexpected count");
            else if (!items.Any(x => Collection.Contains(x)))
                Assert.Fail("Was unable to add all items");

            Assert.Pass();
        }

        [Test]
        public async Task AddRangeParallel()
        {
            Collection.Clear();
            if (Collection.Count != 0)
                Assert.Fail("Could not clear collection");

            ConcurrentBag<int> numbers = [];
            while (numbers.Sum() < TestOptions.Total)
            {
                int number = Generate.RandomNumber(1, 100);
                int sum = numbers.Sum();
                if (sum + number > TestOptions.Total)
                    number = Generate.RandomNumber(1, TestOptions.Total - sum);

                numbers.Add(number);
            }

            if (numbers.Sum() != TestOptions.Total)
                Assert.Fail("Was unable to add together the correct amount of numbers");

            Parallel.For(0, numbers.Count, ParallelOptions, async i =>
            {
                Collection.AddRange(await GetItems(numbers.ElementAt(i)));
            });

            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");
            else if (Collection.Count != TestOptions.Total)
                Assert.Fail("Collection has an unexpected count");

            Collection.Clear();
            if (Collection.Count != 0)
                Assert.Fail("Could not clear collection");

            Assert.Pass();
        }

        [Test]
        public async Task Clear()
        {
            await FillCollection();
            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");

            Collection.Clear();
            Assert.That(Collection, Is.Empty, "Failed to clear the collection");
        }

        [Test]
        public async Task CopyTo()
        {
            await FillCollection();
            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");

            TestItem[] copy = new TestItem[Collection.Count];
            Collection.CopyTo(copy, 0);

            if (copy.Length != Collection.Count)
                Assert.Fail("Copy has an unexpected length");

            for (int i = 0; i < copy.Length; i++)
            {
                if (copy[i] != Collection[i])
                    Assert.Fail("Range does contain expected element");
            }

            Assert.Pass();
        }

        [Test]
        public async Task IndexOf()
        {
            await FillCollection();
            int index = Generate.RandomNumber(0, Collection.Count - 1);
            int foundIndex = Collection.IndexOf(Collection[index]);

            Assert.That(foundIndex, Is.EqualTo(index), "Indexes don't match");
        }

        [Test]
        public async Task IndexOfParallel()
        {
            ConcurrentBag<int> indexes = [];
            await FillCollection();

            int GetIndex()
            {
                lock (indexes)
                {
                    int index = Generate.RandomNumber(0, Collection.Count - 1, indexes, Faker);
                    indexes.Add(index);

                    return index;
                }
            }

            IEnumerable<Task> tasks = Enumerable.Range(0, 50).Select(x =>
            {
                return Task.Run(async () =>
                {
                    await Slim.WaitAsync();
                    try
                    {
                        int index = GetIndex();
                        int foundIndex = Collection.IndexOf(Collection[index]);

                        if (foundIndex != index)
                            Assert.Fail("Indexes don't match");
                    }
                    finally
                    {
                        Slim.Release();
                    }
                });
            });

            await Task.WhenAll(tasks);
            Assert.Pass();
        }

        [Test]
        public async Task Find()
        {
            await FillCollection();
            for (int i = 0; i < Generate.RandomNumber(2, 10, faker: Faker); i++)
            {
                int index = Generate.RandomNumber(0, Collection.Count - 1);
                TestItem found = Collection[index];

                object oldValue = found.Value;
                TestItemValue newValue = GetValue(true);

                Collection.Find(x => x.Id == found.Id)?.Value = newValue;
                if (Collection[index].Value != newValue)
                {
                    Assert.Fail("Find did not return the expected value");
                    return;
                }
            }

            Assert.Pass();
        }

        [Test]
        public async Task FindParallel()
        {
            await FillCollection();

            List<int> indexes = [];
            for (int i = 0; i < Generate.RandomNumber(2, 50, faker: Faker); i++)
            {
                int index = Generate.RandomNumber(0, Collection.Count - 1);
                while (indexes.Contains(index))
                    index = Generate.RandomNumber(0, Collection.Count - 1);

                indexes.Add(index);
            }

            Parallel.For(0, indexes.Count - 1, ParallelOptions, i =>
            {
                int index = indexes[i];
                TestItem found = Collection[index];
                object oldValue = found.Value;
                TestItemValue newValue = GetValue(true);

                Collection.Find(x => x.Id == found.Id)?.Value = newValue;
                if (Collection[index].Value != newValue)
                {
                    Assert.Fail("Find did not return the expected value");
                    return;
                }
            });

            Assert.Pass();
        }

        [Test]
        public async Task Insert()
        {
            await FillCollection();
            TestItem item = GetItem(true);
            int index = Generate.RandomNumber(0, Collection.Count - 1, faker: Faker);
            TestItem oldItem = Collection[index];

            Collection.Insert(index, item);
            if (Collection[index] == oldItem)
                Assert.Fail("Insert failed");
            else if (!Collection.Contains(oldItem))
                Assert.Fail("Collection no longer contains the old item");
            else if (!Collection.Contains(item))
                Assert.Fail("Collection does not contain the new item");

            Assert.That(Collection[index], Is.EqualTo(item), "Unable to get new item");
        }

        [Test]
        public async Task InsertParallel()
        {
            await FillCollection();
            IEnumerable<Task> tasks = Enumerable.Range(0, 50).Select(x =>
            {
                return Task.Run(async () =>
                {
                    await Slim.WaitAsync();
                    try
                    {
                        TestItem item = GetItem(true);
                        int index = Generate.RandomNumber(0, Collection.Count - 1, faker: Faker);
                        TestItem oldItem = Collection[index];

                        Collection.Insert(index, item);
                        if (Collection[index] == oldItem)
                            Assert.Fail("Insert failed");
                        else if (!Collection.Contains(oldItem))
                            Assert.Fail("Collection no longer contains the old item");
                        else if (!Collection.Contains(item))
                            Assert.Fail("Collection does not contain the new item");
                        else if (Collection[index] != item)
                            Assert.Fail("Unable to get new item");
                    }
                    finally
                    {
                        Slim.Release();
                    }
                });
            });

            await Task.WhenAll(tasks);
            Assert.Pass();
        }

        [Test]
        public async Task InsertRange()
        {
            await FillCollection();
            int oldCount = Collection.Count;

            List<TestItem> items = await GetItems(100);
            int index = Generate.RandomNumber(0, Collection.Count - 1, faker: Faker);

            Collection.InsertRange(index, items);
            int newCount = Collection.Count;

            if (Collection.Count == oldCount)
                Assert.Fail("Count has not changed");
            else if (Collection.Count != newCount || Collection.Count != (oldCount + items.Count))
                Assert.Fail("The updated count is not the expected count");
            else if (!items.All(Collection.Contains))
                Assert.Fail("Collection does not contain all of the expected items");

            Assert.Pass();
        }

        [Test]
        public async Task InsertRangeParallel()
        {
            await FillCollection();

            IEnumerable<Task> tasks = Enumerable.Range(0, 50).Select(x =>
            {
                return Task.Run(async () =>
                {
                    await Slim.WaitAsync();
                    try
                    {
                        int oldCount = Collection.Count;
                        List<TestItem> items = await GetItems(10);
                        int index = Generate.RandomNumber(0, Collection.Count - 1, faker: Faker);

                        Collection.InsertRange(index, items);
                        int newCount = Collection.Count;

                        if (Collection.Count == oldCount)
                            Assert.Fail("Count has not changed");
                        else if (Collection.Count != newCount || Collection.Count != (oldCount + items.Count))
                            Assert.Fail("The updated count is not the expected count");
                        else if (!items.All(static x => Collection.Contains(x)))
                            Assert.Fail("Collection does not contain all of the expected items");
                    }
                    finally
                    {
                        Slim.Release();
                    }
                });
            });

            await Task.WhenAll(tasks);
            Assert.Pass();
        }

        [Test]
        public async Task Move()
        {
            await FillCollection();

            ConcurrentBag<int> indexes = [];
            int oldIndex = Generate.RandomNumber(0, Collection.Count - 1, indexes, Faker);
            indexes.Add(oldIndex);

            int newIndex = Generate.RandomNumber(0, Collection.Count - 1, indexes, Faker);
            indexes.Add(newIndex);

            TestItem moveItem = Collection[oldIndex];
            TestItem relocatedItem = Collection[newIndex];

            Collection.Move(oldIndex, newIndex);
            if (!Collection.Contains(relocatedItem))
                Assert.Fail("Collection no longer contains the item that was to be relocated");
            else if (Collection[newIndex] == relocatedItem)
                Assert.Fail("The item that was supposed to be relocated is still at the same index");
            else if (!Collection.Contains(moveItem))
                Assert.Fail("Collection no longer contains the item that was to be moved");
            else if (Collection[oldIndex] == moveItem)
                Assert.Fail("The item that was supposed to be moved is still at the same index");
            else if (Collection[newIndex] != moveItem)
                Assert.Fail("Item has not moved");

            Assert.Pass();
        }

        [Test]
        public async Task Remove()
        {
            await FillCollection();

            int index = Generate.RandomNumber(0, Collection.Count - 1, faker: Faker);
            TestItem item = Collection[index];
            bool removed = Collection.Remove(item);

            if (!removed)
                Assert.Fail("Item could not be removed");
            else if (Collection.Contains(item))
                Assert.Fail("The item that was supposed to be removed still exists");

            Assert.Pass();
        }

        [Test]
        public async Task RemoveParallel()
        {
            await FillCollection();
            ConcurrentBag<int> indexes = [];

            IEnumerable<Task> tasks = Enumerable.Range(0, 50).Select(x =>
            {
                return Task.Run(async () =>
                {
                    await Slim.WaitAsync();
                    try
                    {
                        int index = Generate.RandomNumber(0, Collection.Count - 1, indexes, Faker);
                        indexes.Add(index);

                        TestItem item = Collection[index];
                        bool removed = Collection.Remove(item);

                        if (!removed)
                            Assert.Fail("Item could not be removed");
                        else if (Collection.Contains(item))
                            Assert.Fail("The item that was supposed to be removed still exists");
                    }
                    finally
                    {
                        Slim.Release();
                    }
                });
            });

            await Task.WhenAll(tasks);
            Assert.Pass();
        }

        [Test]
        public async Task RemoveAt()
        {
            await FillCollection();

            int index = Generate.RandomNumber(0, Collection.Count - 1, faker: Faker);
            TestItem item = Collection[index];

            Collection.RemoveAt(index);
            Assert.That(Collection, Does.Not.Contain(item), "The item that was supposed to be removed still exists");
        }

        [Test]
        public async Task RemoveAtParallel()
        {
            await FillCollection();
            ConcurrentBag<int> indexes = [];

            IEnumerable<Task> tasks = Enumerable.Range(0, 50).Select(x =>
            {
                return Task.Run(async () =>
                {
                    await Slim.WaitAsync();
                    try
                    {
                        int index = Generate.RandomNumber(0, Collection.Count - 1, faker: Faker);
                        indexes.Add(index);

                        TestItem item = Collection[index];
                        Collection.RemoveAt(index);
                        if (Collection.Contains(item))
                            Assert.Fail("The item that was supposed to be removed still exists");
                    }
                    finally
                    {
                        Slim.Release();
                    }
                });
            });

            await Task.WhenAll(tasks);
            Assert.Pass();
        }

        [Test]
        public async Task RemoveRange()
        {
            await FillCollection();

            int index = Generate.RandomNumber(0, Collection.Count - 100, faker: Faker);
            int count = Generate.RandomNumber(5, Math.Abs(Collection.Count - index), faker: Faker);
            int oldCount = Collection.Count;

            List<TestItem> removed = Collection.GetRange(index, count);
            Collection.RemoveRange(index, count);

            if (Collection.Count == oldCount)
                Assert.Fail("Count has not changed");
            else if (Collection.Count != (oldCount - removed.Count))
                Assert.Fail("The updated count is not the expected count");
            else if (removed.Any(Collection.Contains))
                Assert.Fail("Not all of the expected items have been removed");

            Assert.Pass();
        }

        [Test]
        public async Task RemoveRangeParallel()
        {
            await FillCollection();

            IEnumerable<Task> tasks = Enumerable.Range(0, 50).Select(x =>
            {
                return Task.Run(async () =>
                {
                    await Slim.WaitAsync();
                    try
                    {
                        int index = Generate.RandomNumber(0, Collection.Count - 10, faker: Faker);
                        int count = Generate.RandomNumber(5, 10, faker: Faker);
                        int oldCount = Collection.Count;

                        List<TestItem> removed = Collection.GetRange(index, count);
                        Collection.RemoveRange(index, count);

                        if (Collection.Count == oldCount)
                            Assert.Fail("Count has not changed");
                        else if (Collection.Count != (oldCount - removed.Count))
                            Assert.Fail("The updated count is not the expected count");
                        else if (removed.Any(Collection.Contains))
                            Assert.Fail("Not all of the expected items have been removed");
                    }
                    finally
                    {
                        Slim.Release();
                    }
                });
            });

            await Task.WhenAll(tasks);
            Assert.Pass();
        }

        [Test]
        public async Task RemoveAll()
        {
            await FillCollection();

            int oldCount = Collection.Count;
            int toAdd = Generate.RandomNumber(10, Collection.Count);
            List<TestItem> newItems = await GetItems(toAdd);

            Collection.AddRange(newItems);
            if (Collection.Count != (oldCount + toAdd))
                Assert.Fail("The count is not the expected count");

            int removedCount = Collection.RemoveAll(x => newItems.Contains(x));
            if (removedCount != toAdd)
                Assert.Fail("Did not remove the expected amount");
            else if (Collection.Any(x => newItems.Contains(x)))
                Assert.Fail("Not all of the expected elements have been removed");

            Assert.Pass();
        }

        [Test]
        public async Task RemoveAllParallel()
        {
            await FillCollection();

            int oldCount = Collection.Count;
            IEnumerable<Task> tasks = Enumerable.Range(0, 50).Select(x =>
            {
                return Task.Run(async () =>
                {
                    await Slim.WaitAsync();
                    try
                    {
                        int tmpCount = Collection.Count;
                        int toAdd = Generate.RandomNumber(10, 20);
                        List<TestItem> newItems = await GetItems(toAdd);

                        Collection.AddRange(newItems);
                        if (Collection.Count != (tmpCount + toAdd))
                            Assert.Fail("The count is not the expected count");

                        int removedCount = Collection.RemoveAll(x => newItems.Contains(x));
                        if (removedCount != toAdd)
                            Assert.Fail("Did not remove the expected amount");
                        else if (Collection.Any(x => newItems.Contains(x)))
                            Assert.Fail("Not all of the expected elements have been removed");
                    }
                    finally
                    {
                        Slim.Release();
                    }
                });
            });

            await Task.WhenAll(tasks);
            if (Collection.Count != oldCount)
                Assert.Fail("Collection does not have the expected count after the removal");

            Assert.Pass();
        }

        [Test]
        public async Task CollectionChanged()
        {
            Collection.Clear();
            Collection.CollectionChanged += EventListener;

            List<TestItem> items = await GetItems(TestOptions.Total);
            foreach (TestItem item in items)
                Collection.Add(item);

            await FillCollection();
            await WaitForEventListener();

            int oldCount = Collection.Count;
            if (Collection.Count != EventCalls.Count)
                Assert.Fail("The event calls list does not contain the expected amount of event calls (Add)");
            else if (EventCalls.Any(x => x.Args?.NewItems?.Count != 1))
                Assert.Fail("The event calls do not contain the expected amount of items (Add)");
            else if (EventCalls.Any(x => x.Args?.Action != NotifyCollectionChangedAction.Add))
                Assert.Fail("The event calls list does not contain only the expected event actions (Add)");

            EventCalls.Clear();
            for (int i = 0; i < oldCount; i++)
                Collection.RemoveAt(0);

            await WaitForEventListener();
            if (EventCalls.Count != oldCount)
                Assert.Fail("The event calls list does not contain the expected amount of event calls (Remove)");
            else if (EventCalls.Any(x => x.Args?.OldItems?.Count != 1))
                Assert.Fail("The event calls do not contain the expected amount of items (Remove)");
            else if (EventCalls.Any(x => x.Args?.Action != NotifyCollectionChangedAction.Remove))
                Assert.Fail("The event calls list does not contain only the expected event actions (Remove)");

            await FillCollection();
            await WaitForEventListener();

            oldCount = Collection.Count;
            EventCalls.Clear();
            Collection.Clear();
            await WaitForEventListener();

            if (EventCalls.Count != 1)
                Assert.Fail("The event calls list does not contain the expected amount of event calls (Clear)");
            else if (EventCalls.Any(x => x.Args?.Action != NotifyCollectionChangedAction.Reset))
                Assert.Fail("The event calls list does not contain only the expected event actions (Clear)");

            EventCalls.Clear();
            Collection.CollectionChanged -= EventListener;
            await FillCollection();

            if (EventCalls.Count != 0)
                Assert.Fail("The event calls list contains item(s) despite the event listener being removed");

            Assert.Pass();
        }

        [Test]
        public async Task CollectionChangedParallel()
        {
            Collection.Clear();
            Collection.CollectionChanged += EventListener;

            ConcurrentBag<TestItem> items = [.. await GetItems(TestOptions.Total)];
            Parallel.For(0, items.Count, ParallelOptions, index =>
            {
                Collection.Add(items.ElementAt(index));
            });

            await WaitForEventListener();
            if (Collection.Count != EventCalls.Count)
                Assert.Fail("The event calls list does not contain the expected amount of event calls");
            else if (EventCalls.Any(x => x.Args?.NewItems?.Count != 1))
                Assert.Fail("The event calls do not contain the expected amount of items");
            else if (EventCalls.Any(x => x.Args?.Action != NotifyCollectionChangedAction.Add))
                Assert.Fail("The event calls list does not contain only the expected event actions");

            EventCalls.Clear();
            Collection.CollectionChanged -= EventListener;
            Assert.Pass();
        }

        private string GetId(bool unique)
        {
            string id = TestItem.GetId(Faker);
            while (unique && Collection.Any(x => x.Id == id))
                id = TestItem.GetId(Faker);

            return id;
        }

        private TestItemValue GetValue(bool unique)
        {
            TestItemValue value = new(Faker);
            while (unique && Collection.Any(x => x.Value == value))
                value = new TestItemValue(Faker);

            return value;
        }

        private TestItem GetItem(bool unique)
            => new(GetId(unique), GetValue(unique));

        private static async Task<List<TestItem>> GetItems(int count)
        {
            await FillReserve(count);

            List<TestItem> result;
            lock (Reserve)
            {
                result = [.. Reserve.Take(count)];
                Reserve.RemoveAll(result);
            }

            return result;
        }

        private async Task FillCollection(bool unique = true, int? count = null)
        {
            count ??= TestOptions.Total;
            lock (Collection)
            {
                if (Collection.Count != 0)
                {
                    if (unique)
                    {
                        List<TestItem> duplicates = [.. Collection.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key)];
                        Collection.RemoveAll(duplicates.Contains);
                    }

                    if (Collection.Count > count)
                    {
                        int remove = Collection.Count - (int)count;
                        for (int i = 0; i < remove; i++)
                            Collection.Remove(Collection[i]);
                    }

                    if (Collection.Count == count)
                        return;
                    else if (count == TestOptions.Total)
                        count -= Collection.Count;
                }
            }

            await FillReserve((int)count);
            List<TestItem> items = [.. Reserve.Take((int)count)];

            lock (Reserve) lock (Collection)
            {
                Reserve.RemoveAll(items);
                Collection.AddRange(items);
            }
        }

#pragma warning disable NUnit1028 // The non-test method is public
        internal static async Task FillReserve(int? count = null, Faker? faker = null)
#pragma warning restore NUnit1028 // The non-test method is public
        {
            lock (Reserve) lock (Collection)
            {
                faker ??= new Faker();
                if (Reserve.Count == 0 || Reserve.Count <= count)
                    Reserve.AddRange(Generate.TestItems(faker: faker));

                List<TestItem> dups = [.. Reserve.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key)];
                dups.AddRange(Reserve.Where(Collection.Contains));

                Reserve.RemoveAll(dups);
            }

            if (count >= Reserve.Count)
                await FillReserve(count - Reserve.Count, faker);
        }

        private void EventListener(object? sender, NotifyCollectionChangedEventArgs e)
            => EventCalls.Add(new EventListenerData(sender, e));

        /// <summary>
        /// Check if the event listener is done updating.
        /// </summary>
        /// <returns></returns>
        private static async Task<int> WaitForEventListener()
        {
            int count;
            while (true)
            {
                count = EventCalls.Count;
                await Task.Delay(5);
                if (EventCalls.Count == count)
                {
                    count = EventCalls.Count;
                    break;
                }
            }

            return count;
        }
    }
}