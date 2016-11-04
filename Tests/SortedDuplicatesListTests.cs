using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cacher;
using System.Linq;
using System.Collections.Generic;

namespace Tests
{
    [TestClass]
    public class TestSortedDuplicatesList
    {
        [TestInitialize]
        public void Init()
        {
        }

        [TestMethod]
        public void InsertsItemsInOrder()
        {
            var list1 = new SortedDuplicatesList<int, int>();
            list1.Add(0, 0);
            list1.Add(2, 2);
            list1.Add(1, 1);
            list1.Add(4, 4);
            list1.Add(3, 3);

            Assert.AreEqual(0, list1.Items[0].Key);
            Assert.AreEqual(1, list1.Items[1].Key);
            Assert.AreEqual(2, list1.Items[2].Key);
            Assert.AreEqual(3, list1.Items[3].Key);
            Assert.AreEqual(4, list1.Items[4].Key);

            var list2 = new SortedDuplicatesList<string, int>();
            list2.Add("b", 0);
            list2.Add("d", 2);
            list2.Add("a", 1);
            list2.Add("c", 4);

            Assert.AreEqual("a", list2.Items[0].Key);
            Assert.AreEqual("b", list2.Items[1].Key);
            Assert.AreEqual("c", list2.Items[2].Key);
            Assert.AreEqual("d", list2.Items[3].Key);

            var list3 = new SortedDuplicatesList<DateTime, int>();
            var jan = new DateTime(4015, 03, 01);
            var feb = new DateTime(4015, 03, 01);
            var mar = new DateTime(4015, 03, 01);
            var apr = new DateTime(4015, 03, 01);

            list3.Add(feb, 4);
            list3.Add(jan, 4);
            list3.Add(apr, 4);
            list3.Add(mar, 4);

            Assert.AreEqual(jan, list3.Items[0].Key);
            Assert.AreEqual(feb, list3.Items[1].Key);
            Assert.AreEqual(mar, list3.Items[2].Key);
            Assert.AreEqual(apr, list3.Items[3].Key);
        }


        [TestMethod]
        public void AddsDuplicateKeys()
        {
            var list = new SortedDuplicatesList<string, int>();
            list.Add("a", 1);
            list.Add("a", 2);

            Assert.AreEqual(2, list.Items.Count);

            Assert.AreEqual(1, list.Items.Where(x => x.Value == 1).ToList().Count());
            Assert.AreEqual(1, list.Items.Where(x => x.Value == 2).ToList().Count());
        }

        [TestMethod]
        public void RemovesCorrectItem()
        {
            var list = new SortedDuplicatesList<string, int>();

            list.Add("a", 1);
            list.Add("a", 2);
            list.Add("a", 3);

            list.Remove("a", 2);
            Assert.AreEqual(1, list.Items.Where(x => x.Value == 1).ToList().Count());
            Assert.AreEqual(0, list.Items.Where(x => x.Value == 2).ToList().Count());
            Assert.AreEqual(1, list.Items.Where(x => x.Value == 3).ToList().Count());
        }

        [TestMethod]
        public void IteratesOverEveryItem()
        {
            var list = new SortedDuplicatesList<string, int>();

            list.Add("a", 1);
            list.Add("a", 2);
            list.Add("a", 3);
            var possibleValues = new List<int>() { 1, 2, 3 };
            foreach (var kvp in list)
            {
                Assert.IsTrue(possibleValues.Contains(kvp.Value));
            }
        }
    }
}
