using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cacher;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace Tests
{
    [TestClass]
    public class CacherTests
    {
        private Cache cache;
        [TestInitialize]
        public void Init()
        {
            this.cache = new Cache();
        }

        [TestMethod]
        public void ResolveCallsResolverOnlyOnce()
        {
            var firstWasCalled = false;
            var secondWasCalled = false;
            var name = this.cache.Resolve("name", () =>
            {
                firstWasCalled = true;
                return "Adam";
            });

            Assert.AreEqual("Adam", name);

            //already has item in cache called name, so resolver won't be called again
            name = this.cache.Resolve("name", () =>
            {
                secondWasCalled = true;
                return "Bob";
            });

            Assert.AreEqual("Adam", name);
            Assert.AreNotEqual("Bob", name);

            Assert.IsTrue(firstWasCalled);
            Assert.IsFalse(secondWasCalled);
        }

        [TestMethod]
        public void AccessLikeDictionary()
        {
            cache.Resolve("name", () =>
            {
                return "Bob";
            });

            Assert.AreEqual("Bob", cache["name"]);
        }

        [TestMethod]
        public void Get()
        {
            cache.Resolve("name", () =>
            {
                return "Bob";
            });
            Assert.AreEqual("Bob", cache.Get("name"));
            Assert.AreEqual("Bob", cache.Get<string>("name"));
        }

        [TestMethod]
        public void Set()
        {
            //set when it's not there.
            cache.Add("name", "Bob");
            Assert.AreEqual("Bob", cache["name"]);
            cache.Add("name", "Adam");
            Assert.AreEqual("Adam", cache["name"]);
        }

        [TestMethod]
        public void SetResetsTimer()
        {
            cache.Add("name", "Bob", 10);
            Assert.AreEqual(10, Math.Ceiling(cache.GetSecondsRemaining("name")));

            cache.Add("name", "Bob", 20);
            Assert.AreEqual(20, Math.Ceiling(cache.GetSecondsRemaining("name")));
        }

        [TestMethod]
        public void GetSecondsRemainingThrowsForUnknownKey()
        {
            cache.Add("name", "Bob");
            try
            {
                cache.GetSecondsRemaining("nonexistant key");
                Assert.Fail("cache should have thrown an exception when accessing an unknown key");
            }
            catch (Exception)
            {
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void ResolveEvictsExpiredItem()
        {
            //add an item that will expire immediately
            cache.Add("name", "Bob", -10);
            var wasCalled = false;
            var value = cache.Resolve("name", () =>
            {
                wasCalled = true;
                return "Jim";
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual("Jim", value);
        }

        [TestMethod]
        public void Reset()
        {
            var name = cache.Resolve("name", () =>
            {
                return "Bob";
            }, 1);
            Assert.AreEqual("Bob", name);

            //timeout for less than a second. 
            Thread.Sleep(500);

            //reset the cache item, giving it exactly one second to live
            cache.Reset("name");
            Thread.Sleep(900);

            name = cache.Resolve("name", () =>
            {
                return "Not Bob";
            }, 1);
            Assert.AreEqual("Bob", name);

            //sleep until after the item should expire. Verify that the item expired
            Thread.Sleep(200);
            name = cache.Resolve("name", () =>
            {
                return "Not Bob";
            }, 1);
            Assert.AreEqual("Not Bob", name);
        }
        [TestMethod]
        public void ResetInfiniteItem()
        {
            var name = cache.Resolve("name", () =>
            {
                return "Bob";
            }, null);
            Assert.AreEqual("Bob", name);

            //timeout for less than a second. 
            Thread.Sleep(500);

            //reset the cache item
            cache.Reset("name");
            Thread.Sleep(900);

            name = cache.Resolve("name", () =>
            {
                return "Not Bob";
            }, 1);
            Assert.AreEqual("Bob", name);
        }

        [TestMethod]
        public void ItemsAreEvictedOnAccess()
        {
            var name = cache.Resolve("name", () =>
            {
                return "Bob";
            }, 1);
            Assert.AreEqual("Bob", name);

            //timeout for less than a second. Name should be the same
            Thread.Sleep(500);
            name = cache.Resolve("name", () =>
            {
                return "Not Bob";
            }, 1);
            Assert.AreEqual("Bob", name);

            //sleep until after the item should expire. Verify that the item expired
            Thread.Sleep(600);
            name = cache.Resolve("name", () =>
            {
                return "Not Bob";
            }, 1);
            Assert.AreEqual("Not Bob", name);
        }

        [TestMethod]
        public void EvictionsHappenDuringAnyAccess()
        {
            var testCache = new TestCache();
            testCache.Add("age", 20);

            //add an immediately expired item
            testCache.Add("name", "Bob", -1);

            //it should be in the internal cache
            Assert.IsTrue(testCache.theCache.ContainsKey("name"));

            //accessing a different property of the cache causes it to clean house
            Assert.IsTrue(testCache.ContainsKey("age"));

            //it should be gone now
            Assert.IsFalse(testCache.theCache.ContainsKey("name"));

        }

        [TestMethod]
        public void ResolveCallsResolverOnlyOnceMultiThreaded()
        {
            //try this operation many times, to attempt to hit certain race conditions
            for (var i = 0; i < 2000; i++)
            {
                var callCount = 0;
                var tasks = new List<Task>();
                for (int j = 0; j < 30; j++)
                {

                    tasks.Add(Task.Factory.StartNew((Object obj) =>
                    {
                        cache.Resolve("name", () =>
                        {
                            callCount++;
                            return "bob";
                        });
                    }, j));
                }
                Task.WaitAll(tasks.ToArray());

                //only one of the tasks should have had their resolve function called
                Assert.AreEqual(1, callCount);

                //remove the item so we can start on the next ieration
                cache.Remove("name");
            }
        }
    }

    class TestCache: Cache
    {
        public ConcurrentDictionary<string, Lazy<CacheItem>> theCache
        {
            get
            {
                return base.cache;
            }
        }
    }
}
