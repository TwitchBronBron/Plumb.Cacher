using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Plumb.Cacher
{
    public class Cache
    {
        public Cache(int? defaultMillisecondsToLive = null)
        {
            this.defaultMillisecondsToLive = defaultMillisecondsToLive;
            this.cache = new ConcurrentDictionary<string, Lazy<CacheItem>>();
        }

        /// <summary>
        /// The collection that actually contains the cached items.
        /// </summary>
        protected virtual ConcurrentDictionary<string, Lazy<CacheItem>> cache { get; set; }

        /// <summary>
        /// A list of all current cache item keys sorted by their eviction date.
        /// </summary>
        protected virtual SortedDuplicatesList<DateTime, CacheItem> evictionOrderList { get; set; } = new SortedDuplicatesList<DateTime, CacheItem>();

        /// <summary>
        /// The default number of seconds that an item should remain in cache before being evicted. 
        /// null means items remain in cache indefinitely (until the cache class is garbage collected)
        /// </summary>
        protected virtual int? defaultMillisecondsToLive { get; set; }

        /// <summary>
        /// Adds or replaces an item in the cache. 
        /// This item will live in cache for the default number of milliseconds.
        /// </summary>
        /// <param name="key">The unique key used to identify this item</param>
        /// <param name="value">The item to be cached</param>
        public void AddOrReplace(string key, object value)
        {
            this.AddOrReplace(key, value, this.defaultMillisecondsToLive);
        }

        /// <summary>
        /// Adds or replaces an item in the cache.
        /// </summary>
        /// <param name="key">The unique key used to identify this item</param>
        /// <param name="value">The item to be cached</param>
        /// <param name="millisecondsToLive">
        ///     The number of milliseconds that this item should remain in the cache. 
        ///     If null, the item will live in cache indefinitely 
        /// </param>
        public void AddOrReplace(string key, object value, int? millisecondsToLive)
        {
            lock (this.cache)
            {
                //remove the existing item, if one exists
                this.Remove(key);
                this.Resolve(key, () =>
                {
                    return value;
                }, millisecondsToLive);
            }
        }

        /// <summary>
        /// Determines if the cache contains an item with the specified key. 
        /// It is not recommended to use ContainsKey in combination with Add() 
        /// because in multi-threaded environments, this could lead to some unexpected results 
        /// due to race conditions. Consider using Resolve() instead.
        /// 
        /// If the item has expired, the cache will NOT contain it
        /// </summary>
        /// <param name="key">The unique key used to identify the item</param>
        /// <returns></returns>
        public virtual bool ContainsKey(string key)
        {
            this.EvictExpiredItems();
            return this.cache.ContainsKey(key);
        }

        /// <summary>
        /// Evicts all expired items from the cache. 
        /// This is called automatically by every internal public method.
        /// It is unlikely that this method will need to be called externally.
        /// 
        /// If nothing needs to be evicted, this method should be extremely fast.
        /// </summary>
        public virtual void EvictExpiredItems()
        {
            lock (evictionOrderList)
            {
                var itemsToRemove = new List<KeyValuePair<DateTime, CacheItem>>();
                foreach (var kvp in this.evictionOrderList)
                {
                    //if the item is not expired, no other items in this list are expired because the list is orderd. stop now
                    if (kvp.Value.IsExpired == false)
                    {
                        break;
                    }

                    //this item is expired, so evict it
                    itemsToRemove.Add(kvp);
                }
                //now that we have the list of items to remove...remove them
                foreach (var kvp in itemsToRemove)
                {
                    this.evictionOrderList.Remove(kvp);

                    //remove it from the cache
                    Lazy<CacheItem> outValue;
                    this.cache.TryRemove(kvp.Value.Key, out outValue);
                }
            }
        }

        /// <summary>
        /// Get the item with the specified key.
        /// An exception is thrown when no item with the specified key is found.
        /// </summary>
        /// <param name="key">The unique key used to identify the item.</param>
        /// <exception cref="System.Exception">Thrown when no item with the specified key is found.</exception>
        /// <returns></returns>
        public object Get(string key)
        {
            this.EvictExpiredItems();
            return Get<object>(key);
        }

        /// <summary>
        /// Get the item with the specified key. 
        /// An exception is thrown when no item with the specified key is found.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The unique key used to identify the item.</param>
        /// <exception cref="System.Exception">Thrown when no item with the specified key is found.</exception>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            this.EvictExpiredItems();
            return (T)this[key];
        }

        /// <summary>
        /// Get the item with the specified key, or a default value if no item with that key exists
        /// </summary>
        /// <param name="key">The unique key used to identify the item.</param>
        /// <param name="defaultValue">A default value to return if the item was not found</param>
        /// <returns></returns>
        public object Get(string key, object defaultValue)
        {
            return Get<object>(key, defaultValue);
        }

        /// <summary>
        /// Get the item with the specified key, or a default value if no item with that key exists
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The unique key used to identify the item.</param>
        /// <param name="defaultValue">A default value to return if the item was not found</param>
        /// <returns></returns>
        public T Get<T>(string key, T defaultValue)
        {
            //if we know for sure the item isn't in the cache, return the default value
            if (!this.ContainsKey(key))
            {
                return defaultValue;
            }
            else
            {
                //try to get the value. if it has been removed between our ContainsKey call above and the Get call below, catch it and return the default value
                try
                {
                    return Get<T>(key);
                }
                catch (Exception)
                {
                    return defaultValue;
                }
            }
        }

        /// <summary>
        /// Get the number of milliseconds remaining until the item will be evicted. 
        /// All cache items have an eviction date. However, non-expiring cache items (those with millisecondsToLive=null), 
        /// will return a very large millisecondsRemaining value, equating to roughly 10,000 years in the future. 
        /// An exception is thrown when no item with the specify key is found.
        /// </summary>
        /// <param name="key">The unique key used to identify the item</param>
        /// <exception cref="System.Exception">Thrown when no item with the specified key is found</exception>
        /// <returns>The number of milliseconds until the cache item will expire</returns>
        public double GetMillisecondsRemaining(string key)
        {
            this.EvictExpiredItems();
            try
            {
                return this.cache[key].Value.MillisecondsRemaining;
            }
            catch (Exception)
            {
                throw new Exception("No item with key '" + key + "' could be found");
            }
        }

        /// <summary>
        /// Gets the value with specified the key. If no item with that key exists, 
        /// the factory function is called to construct a new value.
        /// If the factory function is called, the item is stored with the default millisecondsToLive.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The unique key used to identify the item</param>
        /// <param name="factory">A factory function that is called when the item is not in the 
        /// cache, and a new copy of the item needs to be generated</param>
        /// <returns>The item from cache</returns>
        public T Resolve<T>(string key, Func<T> factory)
        {
            return Resolve(key, factory, this.defaultMillisecondsToLive);
        }

        /// <summary>
        /// Gets the value with specified the key. If no item with that key exists, 
        /// the factory function is called to construct a new value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The unique key used to identify the item.</param>
        /// <param name="factory">A factory function that is called when the item is not in the 
        /// cache, and a new copy of the item needs to be generated.</param>
        /// <param name="millisecondsToLive">The number of milliseconds that this item should remain in the cache. 
        ///     If null, the item will live in cache indefinitely .
        /// </param>
        /// <returns>The item from cache</returns>
        public T Resolve<T>(string key, Func<T> factory, int? millisecondsToLive)
        {
            this.EvictExpiredItems();

            var createdThisCall = false;
            millisecondsToLive = millisecondsToLive != null ? millisecondsToLive : this.defaultMillisecondsToLive;
            var lazyCacheItem = this.cache.GetOrAdd(key, (string k) =>
             {
                 //only allow a single thread to run this specific resolver function at a time. 
                 var lazyResult = new Lazy<CacheItem>(() =>
                 {
                     createdThisCall = true;
                     var value = factory();
                     return new CacheItem(key, value, millisecondsToLive);
                 }, LazyThreadSafetyMode.ExecutionAndPublication);
                 return lazyResult;
             });

            //force the lazy class to load the value in a separate line, just for debugging purposes
            var cacheItem = lazyCacheItem.Value;

            //add this item to the eviction keys list
            lock (evictionOrderList)
            {
                evictionOrderList.Add(cacheItem.EvictionDate, cacheItem);
            }

            //if the item expired and was NOT created this call, toss it and get a new one
            if (cacheItem.IsExpired && createdThisCall == false)
            {
                this.Remove(key);
                return this.Resolve(key, factory, millisecondsToLive);
            }
            else
            {
                return (T)cacheItem.Value;
            }
        }

        /// <summary>
        /// Immediately removes an item from cache with the specified name. 
        /// If no key with that name exists, no exception will be thrown. 
        /// </summary>
        /// <param name="key">The unique key used to identify the item</param>
        /// <returns>True if an item was removed. 
        /// False if no item was removed because it didn't exist in cache to begin with.</returns>
        public bool Remove(string key)
        {
            this.EvictExpiredItems();
            Lazy<CacheItem> lazy;
            this.cache.TryRemove(key, out lazy);

            if (lazy != null)
            {
                //if we actually removed an item, remove it from the eviction order list
                lock (this.evictionOrderList)
                {
                    this.evictionOrderList.Remove(lazy.Value.EvictionDate, lazy.Value);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Reset the timer on a specific cache item. If it still exists in cache, reset it.
        /// An exception is thrown when no item with the specify key is found.
        /// </summary>
        /// <param name="key">The unique key used to identify the item</param>
        /// <exception cref="System.Exception">Thrown when no item with the specified key is found</exception>
        public void Reset(string key)
        {
            this.EvictExpiredItems();
            //try to reset this cache item's timer. 
            if (this.cache.ContainsKey(key))
            {
                //A race condition could exist that would allow it to be in cache above, but then missing here. So eat any exception where that would happen
                try
                {
                    var cacheItem = this.cache[key].Value;
                    lock (this.evictionOrderList)
                    {
                        evictionOrderList.Remove(cacheItem.EvictionDate, cacheItem);
                        cacheItem.Reset();
                        evictionOrderList.Add(cacheItem.EvictionDate, cacheItem);
                    }
                }
                catch (Exception)
                {
                    throw new Exception("No item with that key could be found");
                }
            }
            else
            {
                throw new Exception("No item with that key could be found");
            }
        }

        /// <summary>
        /// Remove all items from the cache. 
        /// </summary>
        public void Clear()
        {
            this.cache.Clear();
            lock (evictionOrderList)
            {
                this.evictionOrderList.Clear();
            }
        }

        /// <summary>
        /// Get the item from the cache with the specified key. 
        /// An exception is thrown if cache does not contain the specified key.
        /// An exception is thrown when no item with the specify key is found.
        /// </summary>
        /// <param name="index"></param>
        /// <exception cref="System.Exception">Thrown when no item with the specified key is found</exception>
        /// <returns></returns>
        public object this[string index]
        {
            get
            {
                this.EvictExpiredItems();
                try
                {
                    return this.cache[index].Value.Value;
                }
                catch (Exception)
                {
                    throw new Exception("No item with that key was found in the cache");
                }
            }
        }
    }
}
