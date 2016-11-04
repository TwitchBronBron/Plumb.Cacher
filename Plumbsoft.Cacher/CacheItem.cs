using System;

namespace Cacher
{
    public class CacheItem
    {
        public string Key;

        public CacheItem(string key, object value, int? millisecondsToLive)
        {
            this.Key = key;
            this.Value = value;
            this.MillisecondsToLive = millisecondsToLive;

            if (this.MillisecondsToLive != null)
            {
                this._EvictionDate = DateTime.UtcNow.AddMilliseconds(this.MillisecondsToLive.Value);
            }
            Reset();
        }

        /// <summary>
        /// The value of the cache item
        /// </summary>
        public object Value;

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
