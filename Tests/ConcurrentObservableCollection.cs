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
        private static DR.ConcurrentCollections.ConcurrentObservableCollection<string> Collection = [];

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
            string item = Collection[index];

            if (string.IsNullOrWhiteSpace(item))
                Assert.Fail("Item is unexpectedly empty");

            Assert.That(item, Is.EqualTo(Collection.ElementAt(index)), "Item is different");
        }

        [Test]
        public void GetRange()
        {
            FillCollection(false, true);
            if (Collection.Count == 0)
                Assert.Fail("Collection is empty");

            List<string> copy = Collection.GetRange(0, Collection.Count);
            if (copy.Count != Collection.Count)
                Assert.Fail("Collection does not have the expected length");

            foreach (var check in copy)
            {
                if (!Collection.Contains(check))
                    Assert.Fail("Collection does not contain element");
            }

            int index = Generate.RandomNumber(1, Collection.Count - 100, faker: _faker);
            int count = Generate.RandomNumber(5, Collection.Count - index, faker: _faker);
            List<string> range = Collection.GetRange(index, count);

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

            List<string> items = GetItems(_testOptions.Total, true);
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

            string[] copy = new string[Collection.Count];
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

            Parallel.For(0, 100, _parallelOptions, i =>
            {
                int index = GetIndex();
                int foundIndex = Collection.IndexOf(Collection[index]);

                if (foundIndex != index)
                    Assert.Fail("Indexes don't match");
            });

            Assert.Pass();
        }

        [Test]
        public void Insert()
        {
            FillCollection(false, true);
            string item = GetItem(true);
            int index = Generate.RandomNumber(0, Collection.Count - 1, faker: _faker);
            string oldItem = Collection[index];

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
            Parallel.For(0, 100, _parallelOptions, i =>
            {
                lock (Collection)
                {
                    string item = GetItem(true);
                    int index = Generate.RandomNumber(0, Collection.Count - 1, faker: _faker);
                    string oldItem = Collection[index];

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

            List<string> items = GetItems(100, true);
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
            Parallel.For(0, 100, _parallelOptions, i =>
            {
                lock (Collection)
                {
                    int oldCount = Collection.Count;
                    List<string> items = GetItems(10, true);
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

            string moveItem = Collection[oldIndex];
            string relocatedItem = Collection[newIndex];

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
            string item = Collection[index];
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

            Parallel.For(0, 100, _parallelOptions, i =>
            {
                lock (Collection)
                {
                    int index = Generate.RandomNumber(0, Collection.Count - 1, indexes, _faker);
                    indexes.Add(index);

                    string item = Collection[index];
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
            string item = Collection[index];

            Collection.RemoveAt(index);
            Assert.That(Collection, Does.Not.Contain(item), "The item that was supposed to be removed still exists");
        }

        [Test]
        public void RemoveAtParallel()
        {
            FillCollection(true, true);
            ConcurrentBag<int> indexes = [];

            Parallel.For(0, 100, _parallelOptions, i =>
            {
                lock (Collection)
                {
                    int index = Generate.RandomNumber(0, Collection.Count - 1, faker: _faker);
                    indexes.Add(index);

                    string item = Collection[index];
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

            List<string> removed = Collection.GetRange(index, count);
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

            Parallel.For(0, 100, _parallelOptions, i =>
            {
                lock (Collection)
                {
                    int index = Generate.RandomNumber(0, Collection.Count - 10, faker: _faker);
                    int count = Generate.RandomNumber(5, 10, faker: _faker);
                    int oldCount = Collection.Count;

                    List<string> removed = Collection.GetRange(index, count);
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
            string guid = GetGuidItem(true);
            List<string> guids = [];

            for (int i = 0; i < Generate.RandomNumber(10, Collection.Count); i++)
                guids.Add(guid);

            Collection.AddRange(guids);
            if (Collection.Count(x => x == guid) != guids.Count)
                Assert.Fail("The GUID count is not the expected count");

            int removedCount = Collection.RemoveAll(x => x == guid);
            if (removedCount != guids.Count)
                Assert.Fail("Did not remove the expected amount");
            else if (Collection.Count != oldCount)
                Assert.Fail("Collection does not have the expected count after the removal");
            else if (Collection.Contains(guid))
                Assert.Fail("Not all of the expected GUIDs have been removed");

            Assert.Pass();
        }

        [Test]
        public void RemoveAllParallel()
        {
            FillCollection(true, true);

            int oldCount = Collection.Count;
            Parallel.For(0, 100, _parallelOptions, x =>
            {
                lock (Collection)
                {
                    string guid = GetGuidItem(true);
                    List<string> guids = [];

                    for (int i = 0; i < Generate.RandomNumber(10, Collection.Count); i++)
                        guids.Add(guid);

                    Collection.AddRange(guids);
                    if (Collection.Count(x => x == guid) != guids.Count)
                        Assert.Fail("The GUID count is not the expected count");

                    int removedCount = Collection.RemoveAll(x => x == guid);
                    if (removedCount != guids.Count)
                        Assert.Fail("Did not remove the expected amount");
                    else if (Collection.Contains(guid))
                        Assert.Fail("Not all of the expected GUIDs have been removed");
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

            ConcurrentBag<string> items = [.. GetItems(_testOptions.Total, true)];
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

        private string GetItem(bool unique)
        {
            string text = Generate.RandomString(100, 1000, _faker);
            if (unique)
            {
                while (Collection.Contains(text))
                    text = Generate.RandomString(100, 1000, _faker);
            }

            return text;
        }

        private string GetGuidItem(bool unique)
        {
            string text = Generate.RandomGuid(_faker).ToString();
            if (unique)
            {
                while (Collection.Contains(text))
                    text = Generate.RandomGuid(_faker).ToString();
            }

            return text;
        }

        private List<string> GetItems(int count, bool unique)
        {
            ConcurrentBag<string> result = [];

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
                    List<string> duplicates = [.. Collection.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key)];
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
            }

            ConcurrentBag<string> items = [.. GetItems((int)count, unique)];
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