using System.Collections.ObjectModel;

namespace DR.ConcurrentCollections
{
    public static class Extensions
    {
        /// <summary>
        /// Add a range of items to a <see cref="ObservableCollection{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="items"></param>
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            for (int i = 0; i < items.Count(); i++)
                collection.Add(items.ElementAt(i));
        }

        /// <summary>
        /// Inserts a list of elements into the <see cref="ObservableCollection{T}"/> starting at the specified index.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="index"></param>
        /// <param name="items"></param>
        public static void InsertRange<T>(this ObservableCollection<T> collection, int index, IEnumerable<T> items)
        {
            try
            {
                for (int i = 0; i < items.Count(); i++)
                {
                    collection.Insert(index, items.ElementAt(i));
                    index++;
                }
            }
            catch
            {
                throw;
            }
        }
    }
}
