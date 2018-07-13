using System;
using System.Collections;
using System.Collections.Generic;

namespace Plumb.Cacher
{
    /// <summary>
    /// A sorted list that allows duplicates
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class SortedDuplicatesList<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : IComparable
    {
        private List<KeyValuePair<TKey, TValue>> list;
        private IComparer<KeyValuePair<TKey, TValue>> comparer;

        /// <summary>
        /// Parameterless constructor
        /// </summary>
        public SortedDuplicatesList()
        {
            list = new List<KeyValuePair<TKey, TValue>>();
            comparer = new KvpKeyComparer<TKey, TValue>();
        }

        /// <summary>
        /// Add a new item with the specified key and value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            var kvp = new KeyValuePair<TKey, TValue>(key, value);
            var index = list.BinarySearch(kvp, comparer);
            //get the index of the item with this exact key, or the index of the closest item
            if (index < 0) index = ~index;
            list.Insert(index, kvp);
        }

        /// <summary>
        /// Clear the list
        /// </summary>
        public void Clear()
        {
            this.list.Clear();
        }

        /// <summary>
        /// All of the items in this list
        /// </summary>
        public List<KeyValuePair<TKey, TValue>> Items
        {
            get
            {
                return list;
            }
        }

        /// <summary>
        /// Remove the item with the specified key and value. 
        /// </summary>
        /// <param name="kvp"></param>
        public void Remove(KeyValuePair<TKey, TValue> kvp)
        {
            var idx = list.BinarySearch(kvp, comparer);
            if (idx > -1)
            {
                list.RemoveAt(idx);
            }
        }

        /// <summary>
        /// Remove the item with the specified key and value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Remove(TKey key, TValue value)
        {
            var kvp = new KeyValuePair<TKey, TValue>(key, value);
            Remove(kvp);
        }

        /// <summary>
        /// Get the enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Get the item at the specified index
        /// </summary>
        /// <value></value>
        public object this[int index]
        {
            get
            {
                return this.list[index];
            }
        }
    }

    /// <summary>
    /// Compare the results of KVP keys
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class KvpKeyComparer<TKey, TValue> : IComparer<KeyValuePair<TKey, TValue>>
     where TKey : IComparable
    {
        /// <summary>
        /// Basic Constructor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(KeyValuePair<TKey, TValue> x, KeyValuePair<TKey, TValue> y)
        {
            return x.Key.CompareTo(y.Key);
        }
    }

}
