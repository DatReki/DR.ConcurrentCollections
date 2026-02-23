using System.Collections;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace DR.ConcurrentCollections
{
    /// <summary>
    /// Represents a thread safe dynamic data collection that provides notifications when items get added or removed, or when the whole list is refreshed.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public sealed class ConcurrentObservableCollection<T> : IList<T>, IReadOnlyList<T>, IList, INotifyCollectionChanged
    {
        private readonly Lock _lock = LockFactory.Create();

        private ImmutableList<T> _items = [];
        private readonly ObservableCollection<T>? _observableCollection;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public ConcurrentObservableCollection() : base()
            => _observableCollection = [];

        public ConcurrentObservableCollection(ObservableCollection<T>? items)
            => _observableCollection = items;

        public ConcurrentObservableCollection(IEnumerable<T> items)
            => _observableCollection = [.. items];

        public ConcurrentObservableCollection(IList<T> items)
            => _observableCollection = [.. items];

        bool ICollection<T>.IsReadOnly => false;

        public int Count => _items.Count;

        bool IList.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        int ICollection.Count => Count;

        object ICollection.SyncRoot => ((ICollection)_items).SyncRoot;

        bool ICollection.IsSynchronized => ((ICollection)_items).IsSynchronized;

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        object? IList.this[int index]
        {
            get => this[index];
            set
            {
                try
                {
                    this[index] = (T)value!;
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get => _items[index];
            set
            {
                lock (_lock)
                {
                    if (_observableCollection != null)
                    {
                        _items = _items.SetItem(index, value);
                        _observableCollection[index] = value;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a shallow copy of a range of elements in the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which the range starts.</param>
        /// <param name="count">The number of elements in the range.</param>
        /// <returns></returns>
        public List<T> GetRange(int index, int count)
        {
            List<T> result = [];

            lock (_lock)
            {
                for (int i = 0; i < count; i++)
                    result.Add(_items[index + i]);
            }

            return result;
        }

        /// <summary>
        /// an item to the collection.
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            lock (_lock)
            {
                _items = _items.Add(item);
                _observableCollection?.Add(item);
                TriggerCollectionChanged(this, NotifyCollectionChangedAction.Add, item);
            }
        }

        /// <summary>
        /// Move item at <paramref name="oldIndex"/> to <paramref name="newIndex"/>.
        /// </summary>
        /// <param name="oldIndex"></param>
        /// <param name="newIndex"></param>
        public void Move(int oldIndex, int newIndex)
        {
            lock (_lock)
            {
                T removedItem = _items[oldIndex];
                _items = _items.RemoveAt(oldIndex);
                _observableCollection?.RemoveAt(oldIndex);

                _items = _items.Insert(newIndex, removedItem);
                _observableCollection?.Insert(newIndex, removedItem);

                TriggerCollectionChanged(this, NotifyCollectionChangedAction.Move, removedItem, newIndex, oldIndex);
            }
        }

        /// <summary>
        /// Add multiple items to the collection.
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(params T[] items)
            => AddRange((IEnumerable<T>)items);

        /// <summary>
        /// Add multiple items to the collection.
        /// </summary>
        /// <param name="items"></param>
        public void AddRange(IEnumerable<T> items)
        {
            lock (_lock)
            {
                _items = _items.AddRange(items);
                _observableCollection?.AddRange(items);
                TriggerCollectionChanged(this, NotifyCollectionChangedAction.Add, items);
            }
        }

        /// <summary>
        /// Inserts multiple items into the collection at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="items"></param>
        public void InsertRange(int index, params T[] items)
            => InsertRange(index, (IEnumerable<T>)items);

        /// <summary>
        /// Inserts multiple items into the collection at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="items"></param>
        public void InsertRange(int index, IEnumerable<T> items)
        {
            lock (_lock)
            {
                _items = _items.InsertRange(index, items);
                _observableCollection?.InsertRange(index, items);
                TriggerCollectionChanged(this, NotifyCollectionChangedAction.Add, items, index);
            }
        }

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _items = _items.Clear();
                _observableCollection?.Clear();
                TriggerCollectionChanged(this, NotifyCollectionChangedAction.Reset);
            }
        }

        /// <summary>
        /// Inserts an item into the collection at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, T item)
        {
            lock (_lock)
            {
                _items = _items.Insert(index, item);
                _observableCollection?.Insert(index, item);
                TriggerCollectionChanged(this, NotifyCollectionChangedAction.Add, item, index);
            }
        }

        /// <summary>
        /// Removes the element with the specified value from the collection.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>
        /// <see langword="true"/> if the item was removed; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Remove(T item)
        {
            lock (_lock)
            {
                ImmutableList<T> newList = _items.Remove(item);
                if (_items != newList)
                {
                    _items = newList;
                    _observableCollection?.Remove(item);
                    TriggerCollectionChanged(this, NotifyCollectionChangedAction.Remove, item);

                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Removes the element at the specified index of the collection.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            lock (_lock)
            {
                _items = _items.RemoveAt(index);
                _observableCollection?.RemoveAt(index);
                TriggerCollectionChanged(this, NotifyCollectionChangedAction.Remove, index);
            }
        }

        /// <summary>
        /// Removes a range of elements from the collection.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="count"></param>
        public void RemoveRange(int index, int count)
        {
            lock (_lock)
            {
                for (int i = 0; i < count; i++)
                    RemoveAt(index);
            }
        }

        /// <summary>
        /// Removes all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public int RemoveAll(Predicate<T> match)
        {
            int result = 0;

            lock (_lock)
            {
                List<T> items = _items.ToList().FindAll(match);
                if (items.Count == 0)
                    return result;
                else
                    result = items.Count;

                ImmutableList<T> newList = _items.RemoveAll(match);
                if (_items != newList)
                {
                    _items = newList;
                    for (int i = 0; i < items.Count; i++)
                    {
                        T? item = items.ElementAt(i);
                        _observableCollection?.Remove(item);
                    }

                    TriggerCollectionChanged(this, NotifyCollectionChangedAction.Remove, items);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the immutable collection.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
            => _items.GetEnumerator();

        /// <summary>
        /// Determines the index of a specific item in the collection.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>The index of <paramref name="item"/> if found in the collection; otherwise, -1.</returns>
        public int IndexOf(T item)
            => _items.IndexOf(item);

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the first occurrence within the entire collection.
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public T? Find(Predicate<T> match)
            => _items.Find(match);

        /// <summary>
        /// Determines whether the collection contains the specific <paramref name="item"/>.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>
        /// <see langword="true"/> if the item is found; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Contains(T item)
            => _items.Contains(item);

        /// <summary>
        /// Copies the entire collection to a compatible one-dimensional array, starting at the specified array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from collection.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
            => _items.CopyTo(array, arrayIndex);

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        int IList.Add(object? value)
        {
            try
            {
                var item = (T)value!;
                lock (_lock)
                {
                    int index = _items.Count;
                    _items = _items.Add(item);
                    _observableCollection?.Add(item);
                    TriggerCollectionChanged(this, NotifyCollectionChangedAction.Add, index);

                    return index;
                }
            }
            catch
            {
                throw;
            }
        }

        bool IList.Contains(object? value)
        {
            if (IsCompatibleObject(value))
                return Contains((T)value!);

            return false;
        }

        void IList.Clear()
            => Clear();

        int IList.IndexOf(object? value)
        {
            if (IsCompatibleObject(value))
                return IndexOf((T)value!);

            return -1;
        }

        void IList.Insert(int index, object? value)
        {
            try
            {
                Insert(index, (T)value!);
            }
            catch
            {
                throw;
            }
        }

        void IList.Remove(object? value)
        {
            if (IsCompatibleObject(value))
                Remove((T)value!);
        }

        void IList.RemoveAt(int index)
            => RemoveAt(index);

        void ICollection.CopyTo(Array array, int index)
            => ((ICollection)_items).CopyTo(array, index);

        private void TriggerCollectionChanged(object sender, NotifyCollectionChangedAction action)
            => CollectionChanged?.Invoke(sender, new NotifyCollectionChangedEventArgs(action));

        private void TriggerCollectionChanged(object sender, NotifyCollectionChangedAction action, object? item)
            => CollectionChanged?.Invoke(sender, new NotifyCollectionChangedEventArgs(action, item));

        private void TriggerCollectionChanged(object sender, NotifyCollectionChangedAction action, IList? items)
            => CollectionChanged?.Invoke(sender, new NotifyCollectionChangedEventArgs(action, items));

        private void TriggerCollectionChanged(object sender, NotifyCollectionChangedAction action, object? item, int index)
            => CollectionChanged?.Invoke(sender, new NotifyCollectionChangedEventArgs(action, item, index));

        private void TriggerCollectionChanged(object sender, NotifyCollectionChangedAction action, IList? items, int index)
            => CollectionChanged?.Invoke(sender, new NotifyCollectionChangedEventArgs(action, items, index));

        private void TriggerCollectionChanged(object sender, NotifyCollectionChangedAction action, object? item, int index, int oldIndex)
            => CollectionChanged?.Invoke(sender, new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));

        private void TriggerCollectionChanged(object sender, NotifyCollectionChangedAction action, IList? items, int index, int oldIndex)
            => CollectionChanged?.Invoke(sender, new NotifyCollectionChangedEventArgs(action, items, index, oldIndex));

        private void TriggerCollectionChanged(object sender, NotifyCollectionChangedAction action, object? oldItem, object? newItem, int index)
            => CollectionChanged?.Invoke(sender, new NotifyCollectionChangedEventArgs(action, oldItem, newItem, index));

        private void TriggerCollectionChanged(object sender, NotifyCollectionChangedAction action, IList oldItems, IList newItems, int index)
            => CollectionChanged?.Invoke(sender, new NotifyCollectionChangedEventArgs(action, oldItems, newItems, index));

        private static bool IsCompatibleObject(object? value)
            => (value is T) || (value == null && default(T) == null);
    }
}
