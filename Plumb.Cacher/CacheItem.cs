using System;
using System.Threading;

namespace Plumb.Cacher
{
    public class CacheItem
    {
        public string Key;

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
        /// Indicates whether this cache item was forcefully killed. Helpful in resolve functions when deciding whether to throw an exception or not.
        /// </summary>
        public bool IsKilled = false;

        /// <summary>
        /// The thread used when resolving the value in this item
        /// </summary>
        public Thread ResolveThread;


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
