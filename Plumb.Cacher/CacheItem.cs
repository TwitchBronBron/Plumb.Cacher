using System;
using System.Threading;

namespace Plumb.Cacher
{
    /// <summary>
    /// Holds a single cache value, and keeps track of its state
    /// </summary>
    public class CacheItem
    {
        /// <summary>
        /// The unique key used to reference this item
        /// </summary>
        public string Key;

        /// <summary>
        /// Construct a new cache item.
        /// </summary>
        /// <param name="key">The unique key used to reference this item</param>
        /// <param name="value">The value to be stored in cache (wrapped in a lazy container)</param>
        /// <param name="millisecondsToLive">The number of milliseconds that this cache item should live</param>
        public CacheItem(string key, Lazy<object> value, int? millisecondsToLive)
        {
            this.Key = key;
            this.LazyValue = value;
            this.MillisecondsToLive = millisecondsToLive;

            if (this.MillisecondsToLive != null)
            {
                this._EvictionDate = DateTime.UtcNow.AddMilliseconds(this.MillisecondsToLive.Value);
            }
            Reset();
        }


        internal Lazy<object> LazyValue;

        /// <summary>
        /// The value of the cache item
        /// </summary>
        public object Value
        {
            get
            {
                return LazyValue.Value;
            }
        }

        /// <summary>
        /// Indicates whether this cache item should be discarded. This is true when a manual .Remove() is called on the cache.
        /// </summary>
        public bool ShouldBeDiscarded = false;

        /// <summary>
        /// The number of milliseconds that this cache item should live before being evicted from the cache
        /// Null means infinite
        /// </summary>
        private int? MillisecondsToLive;

        /// <summary>
        /// The number of milliseconds remaining before this item should be evicted from cache
        /// </summary>
        public double MillisecondsRemaining
        {
            get
            {
                return (this._EvictionDate - DateTime.UtcNow).TotalMilliseconds;
            }
        }

        private DateTime _EvictionDate;

        /// <summary>
        /// The date when this cache item should be evicted from the cache
        /// </summary>
        public DateTime EvictionDate
        {
            get
            {
                return _EvictionDate;
            }
        }

        /// <summary>
        /// Determines if this cache item is expired and should be evicted from the cache
        /// </summary>
        public bool IsExpired
        {
            get
            {
                return DateTime.UtcNow > this._EvictionDate;
            }
        }

        /// <summary>
        /// Resets the timer so that this item will have more time before being ejected from the cache
        /// </summary>
        public void Reset()
        {
            if (this.MillisecondsToLive != null)
            {
                this._EvictionDate = DateTime.UtcNow.AddMilliseconds(this.MillisecondsToLive.Value);
            }
            else
            {
                //this item will be evicted in 10,000 years...so essentially never
                this._EvictionDate = DateTime.MaxValue;
            }
        }
    }
}
