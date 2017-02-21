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
        /// The collection that actually contains the cached items
        /// </summary>
        protected virtual ConcurrentDictionary<string, Lazy<CacheItem>> cache { get; set; }

        /// <summary>
        /// A list of all current cache item keys sorted by their eviction date.
        /// </summary>
        protected virtual SortedDuplicatesList<DateTime, CacheItem> evictionOrderList { get; set; } = new SortedDuplicatesList<DateTime, CacheItem>();

        /// <summary>
        /// The default number of seconds that an item should remain in cache before being evicted. 
        /// Null means infinite
        /// </summary>
        protected virtual int? defaultMillisecondsToLive { get; set; }

        /// <summary>
        /// Adds or replaces the item in the cache with this key.
        /// This is not threadsafe, so only use it when you will not have race conditions
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void AddOrReplace(string key, object value, int? millisecondsToLive = null)
        {
            //remove the existing item, if one exists
            this.Remove(key);
            this.Resolve(key, () =>
            {
                return value;
            }, millisecondsToLive);
        }

        /// <summary>
        /// Determines if the cache contains an item with the specified key. It is not recommended to use ContainsKey 
        /// in combination with Add() because in multi-threaded environments, this could lead to some unexpected 
        /// results due to race conditions. Consider using Resolve() instead.
        /// 
        /// If the item has expired, the cache will NOT contain it
        /// </summary>
        /// <param name="key">The key for the item in question</param>
        /// <returns></returns>
        public virtual bool ContainsKey(string key)
        {
            this.EvictExpiredItems();
            return this.cache.ContainsKey(key);
        }

        /// <summary>
        /// Evicts all expired items from the cache. This is called automatically by every internal public method, 
        /// so it is unlikely that it needs to be called externally.
        /// 
        /// If nothing needs to be evicted, this should be extremely fast
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
        /// Get the item with the specified key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object Get(string key)
        {
            this.EvictExpiredItems();
            return Get<object>(key);
        }

        /// <summary>
        /// Get the item with the specified key. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            this.EvictExpiredItems();
            return (T)this[key];
        }

        /// <summary>
        /// Get the number of milliseconds remaining until the item will be evicted
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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
        /// the factory function is called to construct a new value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="factory"></param>
        /// <param name="millisecondsToLive"></param>
        /// <returns></returns>
        public T Resolve<T>(string key, Func<T> factory, int? millisecondsToLive = null)
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
        /// <param name="key"></param>
        public void Remove(string key)
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
            }
        }

        /// <summary>
        /// Reset the timer on a specific cache item. If it still exists in cache, reset it. If it does not
        /// exist in cache, no action is performed.
        /// </summary>
        /// <param name="key"></param>
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
                    lock (evictionOrderList)
                    {
                        evictionOrderList.Remove(cacheItem.EvictionDate, cacheItem);
                    }
                    cacheItem.Reset();
                    lock (evictionOrderList)
                    {
                        evictionOrderList.Add(cacheItem.EvictionDate, cacheItem);
                    }
                }
                catch (Exception)
                {
                    throw new Exception("No item with that key could be found");
                }
            }
        }

        public void Clear()
        {
            this.cache.Clear();
            lock (evictionOrderList)
            {
                this.evictionOrderList.Clear();
            }
        }

        /// <summary>
        /// Allow dictionary-like access to the cache items. Will fail if the item is not there.
        /// </summary>
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
