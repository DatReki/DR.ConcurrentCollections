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
        private Faker _faker;
        private TestOptions _testOptions;
        private ParallelOptions _parallelOptions;
        private static List<EventListenerData> EventCalls = [];
        private static DR.ConcurrentCollections.ConcurrentObservableCollection<Item> Collection = [];

        [SetUp]
        public void Configure()
        {
            _faker = new Faker();
            Collection = [];
            EventCalls = [];
            _testOptions = new TestOptions();
            _parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = _testOptions.MaxDegreeOfParallelism,
            };
        }

        [Test]
        public void Get()
        {
            FillCollection(false, true);
            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");

            int index = Generate.RandomNumber(0, Collection.Count - 1, faker: _faker);
            Item item = Collection[index];

            Assert.That(item, Is.EqualTo(Collection.ElementAt(index)), "Item is different");
        }

        [Test]
        public void GetRange()
        {
            FillCollection(false, true);
            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");

            List<Item> copy = Collection.GetRange(0, Collection.Count);
            if (copy.Count != Collection.Count)
                Assert.Fail("Collection does not have the expected length");

            foreach (var check in copy)
            {
                if (!Collection.Contains(check))
                    Assert.Fail("Collection does not contain element");
            }

            int index = Generate.RandomNumber(1, Collection.Count - 100, faker: _faker);
            int count = Generate.RandomNumber(5, Collection.Count - index, faker: _faker);
            List<Item> range = Collection.GetRange(index, count);

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
        public void Add()
        {
            Collection.Clear();
            if (Collection.Count != 0)
                Assert.Fail("Could not clear collection");

            FillCollection(false, true);
            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");

            Assert.That(_testOptions.Total, Is.EqualTo(Collection.Count), "The collection has an unexpected count");
        }

        [Test]
        public void AddParallel()
        {
            Collection.Clear();
            if (Collection.Count != 0)
                Assert.Fail("Could not clear collection");

            FillCollection(true, true);
            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");

            Assert.That(_testOptions.Total, Is.EqualTo(Collection.Count), "The collection has an unexpected count");
        }

        [Test]
        public void AddRange()
        {
            Collection.Clear();
            if (Collection.Count != 0)
                Assert.Fail("Could not clear collection");

            List<Item> items = GetItems(_testOptions.Total, true);
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
        public void AddRangeParallel()
        {
            Collection.Clear();
            if (Collection.Count != 0)
                Assert.Fail("Could not clear collection");

            ConcurrentBag<int> numbers = [];
            while (numbers.Sum() < _testOptions.Total)
            {
                int number = Generate.RandomNumber(1, 100);
                int sum = numbers.Sum();
                if (sum + number > _testOptions.Total)
                    number = Generate.RandomNumber(1, _testOptions.Total - sum);

                numbers.Add(number);
            }

            if (numbers.Sum() != _testOptions.Total)
                Assert.Fail("Was unable to add together the correct amount of numbers");

            Parallel.For(0, numbers.Count, _parallelOptions, i =>
            {
                Collection.AddRange(GetItems(numbers.ElementAt(i), false));
            });

            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");
            else if (Collection.Count != _testOptions.Total)
                Assert.Fail("Collection has an unexpected count");

            Collection.Clear();
            if (Collection.Count != 0)
                Assert.Fail("Could not clear collection");

            Assert.Pass();
        }

        [Test]
        public void Clear()
        {
            FillCollection(false, true);
            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");

            Collection.Clear();
            Assert.That(Collection, Is.Empty, "Failed to clear the collection");
        }

        [Test]
        public void CopyTo()
        {
            FillCollection(false, true);
            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");

            Item[] copy = new Item[Collection.Count];
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
        public void IndexOf()
        {
            FillCollection(false, true);
            int index = Generate.RandomNumber(0, Collection.Count - 1);
            int foundIndex = Collection.IndexOf(Collection[index]);

            Assert.That(foundIndex, Is.EqualTo(index), "Indexes don't match");
        }

        [Test]
        public void IndexOfParallel()
        {
            ConcurrentBag<int> indexes = [];
            FillCollection(true, true);

            int GetIndex()
            {
                lock (indexes)
                {
                    int index = Generate.RandomNumber(0, Collection.Count - 1, indexes, _faker);
                    indexes.Add(index);

                    return index;
                }
            }

            Parallel.For(0, 50, _parallelOptions, i =>
            {
                int index = GetIndex();
                int foundIndex = Collection.IndexOf(Collection[index]);

                if (foundIndex != index)
                    Assert.Fail("Indexes don't match");
            });

            Assert.Pass();
        }

        [Test]
        public void Find()
        {
            FillCollection(false, true);
            for (int i = 0; i < Generate.RandomNumber(2, 10, faker: _faker); i++)
            {
                int index = Generate.RandomNumber(0, Collection.Count - 1);
                Item found = Collection[index];

                object oldValue = found.Value;
                ItemValue newValue = GetValue(true);

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
        public void FindParallel()
        {
            FillCollection(true, true);

            List<int> indexes = [];
            for (int i = 0; i < Generate.RandomNumber(2, 50, faker: _faker); i++)
            {
                int index = Generate.RandomNumber(0, Collection.Count - 1);
                while (indexes.Contains(index))
                    index = Generate.RandomNumber(0, Collection.Count - 1);

                indexes.Add(index);
            }

            Parallel.For(0, indexes.Count - 1, _parallelOptions, i =>
            {
                int index = indexes[i];
                Item found = Collection[index];
                object oldValue = found.Value;
                ItemValue newValue = GetValue(true);

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
        public void Insert()
        {
            FillCollection(false, true);
            Item item = GetItem(true);
            int index = Generate.RandomNumber(0, Collection.Count - 1, faker: _faker);
            Item oldItem = Collection[index];

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
        public void InsertParallel()
        {
            FillCollection(true, true);
            Parallel.For(0, 50, _parallelOptions, i =>
            {
                lock (Collection)
                {
                    Item item = GetItem(true);
                    int index = Generate.RandomNumber(0, Collection.Count - 1, faker: _faker);
                    Item oldItem = Collection[index];

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
            });

            Assert.Pass();
        }

        [Test]
        public void InsertRange()
        {
            FillCollection(false, true);
            int oldCount = Collection.Count;

            List<Item> items = GetItems(100, true);
            int index = Generate.RandomNumber(0, Collection.Count - 1, faker: _faker);

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
        public void InsertRangeParallel()
        {
            FillCollection(true, true);
            Parallel.For(0, 50, _parallelOptions, i =>
            {
                lock (Collection)
                {
                    int oldCount = Collection.Count;
                    List<Item> items = GetItems(10, true);
                    int index = Generate.RandomNumber(0, Collection.Count - 1, faker: _faker);

                    Collection.InsertRange(index, items);
                    int newCount = Collection.Count;

                    if (Collection.Count == oldCount)
                        Assert.Fail("Count has not changed");
                    else if (Collection.Count != newCount || Collection.Count != (oldCount + items.Count))
                        Assert.Fail("The updated count is not the expected count");
                    else if (!items.All(static x => Collection.Contains(x)))
                        Assert.Fail("Collection does not contain all of the expected items");
                }
            });

            Assert.Pass();
        }

        [Test]
        public void Move()
        {
            FillCollection(false, true);

            ConcurrentBag<int> indexes = [];
            int oldIndex = Generate.RandomNumber(0, Collection.Count - 1, indexes, _faker);
            indexes.Add(oldIndex);

            int newIndex = Generate.RandomNumber(0, Collection.Count - 1, indexes, _faker);
            indexes.Add(newIndex);

            Item moveItem = Collection[oldIndex];
            Item relocatedItem = Collection[newIndex];

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
        public void Remove()
        {
            FillCollection(false, true);

            int index = Generate.RandomNumber(0, Collection.Count - 1, faker: _faker);
            Item item = Collection[index];
            bool removed = Collection.Remove(item);

            if (!removed)
                Assert.Fail("Item could not be removed");
            else if (Collection.Contains(item))
                Assert.Fail("The item that was supposed to be removed still exists");

            Assert.Pass();
        }

        [Test]
        public void RemoveParallel()
        {
            FillCollection(true, true);
            ConcurrentBag<int> indexes = [];

            Parallel.For(0, 50, _parallelOptions, i =>
            {
                lock (Collection)
                {
                    int index = Generate.RandomNumber(0, Collection.Count - 1, indexes, _faker);
                    indexes.Add(index);

                    Item item = Collection[index];
                    bool removed = Collection.Remove(item);

                    if (!removed)
                        Assert.Fail("Item could not be removed");
                    else if (Collection.Contains(item))
                        Assert.Fail("The item that was supposed to be removed still exists");
                }
            });

            Assert.Pass();
        }

        [Test]
        public void RemoveAt()
        {
            FillCollection(false, true);

            int index = Generate.RandomNumber(0, Collection.Count - 1, faker: _faker);
            Item item = Collection[index];

            Collection.RemoveAt(index);
            Assert.That(Collection, Does.Not.Contain(item), "The item that was supposed to be removed still exists");
        }

        [Test]
        public void RemoveAtParallel()
        {
            FillCollection(true, true);
            ConcurrentBag<int> indexes = [];

            Parallel.For(0, 50, _parallelOptions, i =>
            {
                lock (Collection)
                {
                    int index = Generate.RandomNumber(0, Collection.Count - 1, faker: _faker);
                    indexes.Add(index);

                    Item item = Collection[index];
                    Collection.RemoveAt(index);
                    if (Collection.Contains(item))
                        Assert.Fail("The item that was supposed to be removed still exists");
                }
            });

            Assert.Pass();
        }

        [Test]
        public void RemoveRange()
        {
            FillCollection(false, true);

            int index = Generate.RandomNumber(0, Collection.Count - 100, faker: _faker);
            int count = Generate.RandomNumber(5, Math.Abs(Collection.Count - index), faker: _faker);
            int oldCount = Collection.Count;

            List<Item> removed = Collection.GetRange(index, count);
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
        public void RemoveRangeParallel()
        {
            FillCollection(true, true);

            Parallel.For(0, 50, _parallelOptions, i =>
            {
                lock (Collection)
                {
                    int index = Generate.RandomNumber(0, Collection.Count - 10, faker: _faker);
                    int count = Generate.RandomNumber(5, 10, faker: _faker);
                    int oldCount = Collection.Count;

                    List<Item> removed = Collection.GetRange(index, count);
                    Collection.RemoveRange(index, count);

                    if (Collection.Count == oldCount)
                        Assert.Fail("Count has not changed");
                    else if (Collection.Count != (oldCount - removed.Count))
                        Assert.Fail("The updated count is not the expected count");
                    else if (removed.Any(Collection.Contains))
                        Assert.Fail("Not all of the expected items have been removed");
                }
            });

            Assert.Pass();
        }

        [Test]
        public void RemoveAll()
        {
            FillCollection(false, true);

            int oldCount = Collection.Count;
            int toAdd = Generate.RandomNumber(10, Collection.Count);
            List<Item> newItems = GetItems(toAdd, true);

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
        public void RemoveAllParallel()
        {
            FillCollection(true, true);

            int oldCount = Collection.Count;
            Parallel.For(0, 50, _parallelOptions, x =>
            {
                lock (Collection)
                {
                    int tmpCount = Collection.Count;
                    int toAdd = Generate.RandomNumber(10, 20);
                    List<Item> newItems = GetItems(toAdd, true);

                    Collection.AddRange(newItems);
                    if (Collection.Count != (tmpCount + toAdd))
                        Assert.Fail("The count is not the expected count");

                    int removedCount = Collection.RemoveAll(x => newItems.Contains(x));
                    if (removedCount != toAdd)
                        Assert.Fail("Did not remove the expected amount");
                    else if (Collection.Any(x => newItems.Contains(x)))
                        Assert.Fail("Not all of the expected elements have been removed");
                }
            });

            if (Collection.Count != oldCount)
                Assert.Fail("Collection does not have the expected count after the removal");

            Assert.Pass();
        }

        [Test]
        public async Task CollectionChanged()
        {
            Collection.Clear();
            Collection.CollectionChanged += EventListener;

            FillCollection(false, true);
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

            FillCollection(false, true);
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
            FillCollection(false, true);

            if (EventCalls.Count != 0)
                Assert.Fail("The event calls list contains item(s) despite the event listener being removed");

            Assert.Pass();
        }

        [Test]
        public async Task CollectionChangedParallel()
        {
            Collection.Clear();
            Collection.CollectionChanged += EventListener;

            ConcurrentBag<Item> items = [.. GetItems(_testOptions.Total, true)];
            Parallel.For(0, items.Count, _parallelOptions, index =>
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
            string id = Item.GetId(_faker);
            while (unique && Collection.Any(x => x.Id == id))
                id = Item.GetId(_faker);

            return id;
        }

        private ItemValue GetValue(bool unique)
        {
            ItemValue value = new(_faker);
            while (unique && Collection.Any(x => x.Value == value))
                value = new ItemValue(_faker);

            return value;
        }

        private Item GetItem(bool unique)
            => new(GetId(unique), GetValue(unique));

        private List<Item> GetItems(int count, bool unique)
        {
            ConcurrentBag<Item> result = [];

            for (int i = 0; i < count; i++)
                result.Add(GetItem(unique));

            return [.. result];
        }

        private void FillCollection(bool parallel, bool unique = true, int? count = null)
        {
            count ??= _testOptions.Total;
            if (Collection.Count != 0)
            {
                if (unique)
                {
                    List<Item> duplicates = [.. Collection.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key)];
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
                else if (count == _testOptions.Total)
                    count -= Collection.Count;
            }

            ConcurrentBag<Item> items = [.. GetItems((int)count, unique)];
            if (parallel)
            {
                Parallel.For(0, (int)count, _parallelOptions, i =>
                {
                    Collection.Add(items.ElementAt(i));
                });
            }
            else
            {
                for (int i = 0; i < count; i++)
                    Collection.Add(items.ElementAt(i));
            }
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