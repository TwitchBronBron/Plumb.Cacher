using System;

namespace Cacher
{
    public class CacheItem
    {
        public CacheItem(object value, int? secondsToLive)
        {
            this.Value = value;
            this.SecondsToLive = secondsToLive;

            if (this.SecondsToLive != null)
            {
                this.EvictionDate = DateTime.UtcNow.AddSeconds(this.SecondsToLive.Value);
            }
        }

        /// <summary>
        /// The value of the cache item
        /// </summary>
        public object Value;

        /// <summary>
        /// The number of seconds that this cache item should live before being evicted from the cache
        /// Null means infinite
        /// </summary>
        private int? SecondsToLive;

        /// <summary>
        /// The number of seconds remaining before this item should be evicted from cache
        /// </summary>
        public double SecondsRemaining
        {
            get
            {
                return (this.EvictionDate - DateTime.UtcNow).Value.TotalSeconds;
            }
        }

        /// <summary>
        /// The date when this cache item should be evicted from the cache
        /// </summary>
        private DateTime? EvictionDate;

        /// <summary>
        /// Determines if this cache item is expired and should be evicted from the cache
        /// </summary>
        public bool IsExpired
        {
            get
            {
                return DateTime.UtcNow > this.EvictionDate;
            }
        }

        /// <summary>
        /// Resets the timer so that this item will have more time before being ejected from the cache
        /// </summary>
        public void Reset()
        {
            if (this.SecondsToLive != null)
            {
                this.EvictionDate = DateTime.UtcNow.AddSeconds(this.SecondsToLive.Value);
            }
            else
            {
                this.EvictionDate = null;
            }
        }
    }
}
