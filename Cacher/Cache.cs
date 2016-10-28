using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cacher
{
    public class Cache : DynamicObject
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
        /// The default number of seconds that an item should remain in cache before being evicted. 
        /// Null means infinite
        /// </summary>
        protected virtual int? defaultMillisecondsToLive { get; set; }


        /// <summary>
        /// Adds or replaces the item in the cache with this key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, object value, int? millisecondsToLive = null)
        {
            this.EvictExpiredItems();
            this.cache.AddOrUpdate(key, (x) =>
            {
                var lazy = new Lazy<CacheItem>(() =>
                {
                    return new CacheItem(value, millisecondsToLive);
                }, false);

                //immediately access the value
                var lazyValue = lazy.Value;
                return lazy;
            }, (x, y) =>
            {
                var lazy = new Lazy<CacheItem>(() =>
                {
                    return new CacheItem(value, millisecondsToLive);
                }, false);
                //immediately access the value
                var lazyValue = lazy.Value;
                return lazy;
            });
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
        /// so it is unlikely that it needs to be called externally
        /// </summary>
        public virtual void EvictExpiredItems()
        {
            //TODO: if nothing needs evicted, this should be super fast.
            foreach (var kvp in cache)
            {
                if (kvp.Value.Value.IsExpired)
                {
                    ((IDictionary<string, Lazy<CacheItem>>)cache).Remove(kvp.Key);
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
            var cacheItem = this.cache.GetOrAdd(key, (string k) =>
             {
                 //only allow a single thread to run this specific resolver function at a time. 
                 var lazyResult = new Lazy<CacheItem>(() =>
                 {
                     createdThisCall = true;
                     var value = factory();
                     return new CacheItem(value, millisecondsToLive);
                 }, LazyThreadSafetyMode.ExecutionAndPublication);
                 return lazyResult;
             });

            //force the lazy class to load the value in a separate line, just for debugging purposes
            { var value = cacheItem.Value; }

            //if the item expired and was NOT created this call, toss it and get a new one
            if (cacheItem.Value.IsExpired && createdThisCall == false)
            {
                this.Remove(key);
                return this.Resolve(key, factory, millisecondsToLive);
            }
            else
            {
                return (T)cacheItem.Value.Value;
            }
        }

        /// <summary>
        /// Immediately removes an item from cache with the specified name
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            this.EvictExpiredItems();
            IDictionary<string, Lazy<CacheItem>> cache = this.cache;
            cache.Remove(key);
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
                    this.cache[key].Value.Reset();
                }
                catch (Exception)
                {
                    throw new Exception("No item with that key could be found");
                }
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
                return this.cache[index].Value.Value;
            }
        }
    }
}
