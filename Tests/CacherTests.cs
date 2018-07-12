using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plumb.Cacher;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Tests
{
    public class CacherTests
    {
        private Cache cache;
        public CacherTests()
        {
            this.cache = new Cache();
        }

        [Fact]
        public void ResolveCallsResolverOnlyOnce()
        {
            var firstWasCalled = false;
            var secondWasCalled = false;
            var name = this.cache.Resolve("name", () =>
            {
                firstWasCalled = true;
                return "Adam";
            });

            Assert.Equal("Adam", name);

            //already has item in cache called name, so resolver won't be called again
            name = this.cache.Resolve("name", () =>
            {
                secondWasCalled = true;
                return "Bob";
            });

            Assert.Equal("Adam", name);
            Assert.NotEqual("Bob", name);

            Assert.True(firstWasCalled);
            Assert.False(secondWasCalled);
        }

        [Fact]
        public void AccessLikeDictionary()
        {
            cache.Resolve("name", () =>
            {
                return "Bob";
            }, 20000000);

            Assert.Equal("Bob", cache["name"]);
        }

        [Fact]
        public void Get()
        {
            cache.Resolve("name", () =>
            {
                return "Bob";
            });
            Assert.Equal("Bob", cache.Get("name"));
            Assert.Equal("Bob", cache.Get<string>("name"));
        }

        [Fact]
        public void GetThrowsExceptionWhenNotFound()
        {
            try
            {
                var item = cache.Get("something");
                Assert.True(false, "Should have thrown an exception");
            }
            catch (Exception)
            {
                Assert.True(true);
            }

            try
            {
                var item = cache.Get<string>("something");
                Assert.True(false, "Should have thrown an exception");
            }
            catch (Exception)
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void GetWithDefaults()
        {
            //test non-generic
            Assert.Equal(1, cache.Get("something", 1));
            Assert.Equal("cat", cache.Get("something", "cat"));

            //test generic
            Assert.Equal(1, cache.Get<int>("something", 1));
            Assert.Equal("cat", cache.Get<string>("something", "cat"));


        }


        [Fact]
        public void Set()
        {
            //set when it's not there.
            cache.AddOrReplace("name", "Bob");
            Assert.Equal("Bob", cache["name"]);
            cache.AddOrReplace("name", "Adam");
            Assert.Equal("Adam", cache["name"]);
        }

        [Fact]
        public void SetResetsTimer()
        {
            cache.AddOrReplace("name", "Bob", 10);
            //if the test runs slow, the milliseconds won't be exact...so verify that the milliseconds is close
            Assert.InRange(Math.Ceiling(cache.GetMillisecondsRemaining("name")), 5, 10);

            cache.AddOrReplace("name", "Bob", 20);
            //if the test runs slow, the milliseconds won't be exact...so verify that the milliseconds is close
            Assert.InRange(Math.Ceiling(cache.GetMillisecondsRemaining("name")), 15, 20);
        }

        [Fact]
        public void GetMillisecondsRemainingThrowsForUnknownKey()
        {
            cache.AddOrReplace("name", "Bob");
            try
            {
                cache.GetMillisecondsRemaining("nonexistant key");
                Assert.False(true, "cache should have thrown an exception when accessing an unknown key");
            }
            catch (Exception)
            {
                Assert.True(true);
            }
        }

        [Fact]
        public void ResolveEvictsExpiredItem()
        {
            //add an item that will expire immediately
            cache.AddOrReplace("name", "Bob", -10);
            var wasCalled = false;
            var value = cache.Resolve("name", () =>
            {
                wasCalled = true;
                return "Jim";
            });

            Assert.True(wasCalled);
            Assert.Equal("Jim", value);
        }

        [Fact]
        public void Reset()
        {
            var name = cache.Resolve("name", () =>
            {
                return "Bob";
            }, 20);
            Assert.Equal("Bob", name);

            //timeout for less than a second. 
            Thread.Sleep(10);

            //reset the cache item, giving it exactly one second to live
            cache.Reset("name");
            Thread.Sleep(15);

            name = cache.Resolve("name", () =>
            {
                return "Not Bob";
            }, 1);
            Assert.Equal("Bob", name);

            //sleep until after the item should expire. Verify that the item expired
            Thread.Sleep(10);
            name = cache.Resolve("name", () =>
            {
                return "Not Bob";
            }, 1);
            Assert.Equal("Not Bob", name);
        }
        [Fact]
        public void ResetInfiniteItem()
        {
            var name = cache.Resolve("name", () =>
            {
                return "Bob";
            }, null);
            Assert.Equal("Bob", name);

            Thread.Sleep(10);

            //reset the cache item
            cache.Reset("name");
            Thread.Sleep(100);

            name = cache.Resolve("name", () =>
            {
                return "Not Bob";
            }, 1);
            Assert.Equal("Bob", name);
        }

        [Fact]
        public void ItemsAreEvictedOnAccess()
        {
            var name = cache.Resolve("name", () =>
            {
                return "Bob";
            }, 20);
            Assert.Equal("Bob", name);

            //timeout for less than a second. Name should be the same
            Thread.Sleep(10);
            name = cache.Resolve("name", () =>
            {
                return "Not Bob";
            }, 1);
            Assert.Equal("Bob", name);

            //sleep until after the item should expire. Verify that the item expired
            Thread.Sleep(20);
            name = cache.Resolve("name", () =>
            {
                return "Not Bob";
            }, 1);
            Assert.Equal("Not Bob", name);
        }

        [Fact]
        public void EvictionsHappenDuringAnyAccess()
        {
            var testCache = new TestCache();
            testCache.AddOrReplace("age", 20);

            //add an immediately expired item
            testCache.AddOrReplace("name", "Bob", -1);

            //it should be in the internal cache
            Assert.True(testCache.theCache.ContainsKey("name"));

            //accessing a different property of the cache causes it to clean house
            Assert.True(testCache.ContainsKey("age"));

            //it should be gone now
            Assert.False(testCache.theCache.ContainsKey("name"));

        }

        [Fact]
        public void ResolveCallsResolverOnlyOnceMultiThreaded()
        {
            //try this operation many times, to attempt to hit certain race conditions
            for (var i = 0; i < 2000; i++)
            {
                //clear the cache before each run
                cache.Clear();

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
                Assert.Equal(1, callCount);

                //remove the item so we can start on the next ieration
                cache.Remove("name");
            }
        }

        [Fact]
        public void RemoveKillsActiveResolves()
        {
            var exitWhile = false;
            var readerTask = Task.Factory.StartNew((Object obj) =>
            {
                try
                {
                    var value = cache.Resolve<string>("name", () =>
                    {
                        while (exitWhile == false)
                        {
                            Thread.Sleep(10);
                        }
                        return "bob";
                    });
                    Assert.False(true, "Should have thrown an exception");
                }
                catch (Exception)
                {
                    Assert.True(true, "Exception was thrown where it should have been");
                }
            }, null);

            while (cache.ContainsKey("name") == false)
            {
                Thread.Sleep(10);
            }
            //remove the item from cache, terminating all active resolvers for that name
            cache.Remove("name", true);
            while (readerTask.IsCompleted == false)
            {
                Thread.Sleep(10);
            }
        }

        [Fact]
        public void RemoveDoesNotKillWhenResolved()
        {
            cache.Resolve("name", () =>
            {
                return "bob";
            });
            Assert.True(cache.ContainsKey("name"));
            //remove the item from cache, terminating all active resolvers for that name
            cache.Remove("name", true);
            Assert.False(cache.ContainsKey("name"));
        }

        [Fact]
        public void DoesNotDeadlock()
        {
            var exitWhile = false;
            var readerTask = Task.Factory.StartNew((Object obj) =>
            {
                var result = cache.Resolve("name", () =>
                {
                    while (exitWhile == false)
                    {
                        Thread.Sleep(100);
                    }
                    return "bob";
                });
                var k = result;
            }, null);

            //spin until the cache has the key
            while (cache.ContainsKey("name") == false)
            {
                Thread.Sleep(100);
            }
            //remove the item from cache
            cache.Remove("name");
            //exit the reader's while loop
            exitWhile = true;

            //wait for the reader task to complete
            Task.WaitAll(new[] { readerTask });

            //what is in the cache?
            Assert.False(cache.ContainsKey("name"));
        }

        [Fact]
        public void ResolveDoesNotSaveItemWhenExceptionIsThrown()
        {
            Assert.False(cache.ContainsKey("item"));
            try
            {
                cache.Resolve<bool>("item", () =>
                {
                    throw new Exception("AAA");
                });
                Assert.False(true, "should not have run this line");
            }
            catch (Exception)
            {
                Assert.False(cache.ContainsKey("item"), "item should not be included in the cache");
            }
        }

        [Fact]
        public async Task ResolveAsyncWorks()
        {
            var item = await cache.ResolveAsync<bool>("itWorks!", () =>
            {
                return Task.FromResult(true);
            });
            Assert.True(item);
        }

        [Fact]
        public void ResolveCatchesRecursion()
        {
            try
            {
                //call to resolve the same cache within the same cache
                cache.Resolve("something", () =>
                {
                    return cache.Resolve("something", () =>
                    {
                        return true;
                    });
                });
            }
            catch (Exception e)
            {
                Assert.Equal("Possible recursive resolve() detected", e.Message);
            }

            //the previous failure should have removed the cache item
            Assert.Equal(true, cache.Resolve("something", () => { return true; }));

            //regular exceptions should be thrown with their messages
            try
            {
                //call to resolve the same cache within the same cache
                cache.Resolve<int>("somethingElse", () =>
                {
                    throw new Exception("Custom message");
                });
            }
            catch (Exception e)
            {
                Assert.Equal("Custom message", e.Message);
            }
        }

        class TestCache : Cache
        {
            public ConcurrentDictionary<string, Lazy<CacheItem>> theCache
            {
                get
                {
                    return base.InternalCache;
                }
            }
        }
    }
}
