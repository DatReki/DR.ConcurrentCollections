namespace Tests.Core
{
    internal static class Extensions
    {
        internal static Task ForEachAsync<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, Task<TResult>> taskSelector, Action<TSource, TResult> resultProcessor)
        {
            var oneAtATime = new SemaphoreSlim(initialCount: 1, maxCount: 1);
            return Task.WhenAll(
                from item in source
                select ProcessAsync(item, taskSelector, resultProcessor, oneAtATime));
        }

        internal static async Task ProcessAsync<TSource, TResult>(
            TSource item,
            Func<TSource, Task<TResult>> taskSelector, Action<TSource, TResult> resultProcessor,
            SemaphoreSlim oneAtATime)
        {
            TResult result = await taskSelector(item);
            await oneAtATime.WaitAsync();
            try { resultProcessor(item, result); }
            finally { oneAtATime.Release(); }
        }


        internal static bool HasDuplicates<T>(this IEnumerable<T> items)
        {
            HashSet<T> tmp = [];

            for (var i = 0; i < items.Count(); ++i)
            {
                if (!tmp.Add(items.ElementAt(i))) 
                    return true;
            }

            return false;
        }

        internal static void AddRange<T>(this SynchronizedCollection<T> bag, List<T> items)
        {
            lock (bag)
            {
                for (int i = 0; i < items.Count; i++)
                    bag.Add(items[i]);
            }
        }

        internal static void RemoveAll<T>(this SynchronizedCollection<T> bag, List<T> items)
        {
            lock (bag)
            {
                for (int i = 0; i < items.Count; i++)
                    bag.Remove(items[i]);
            }
        }
    }
}
