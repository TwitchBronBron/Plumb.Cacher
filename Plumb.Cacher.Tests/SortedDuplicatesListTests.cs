using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plumb.Cacher;
using Xunit;

namespace Plumb.Cacher.Tests
{
    public class TestSortedDuplicatesList
    {

        [Fact]
        public void GetEnumeratorWorks()
        {
            var list1 = new SortedDuplicatesList<int, int>();
            list1.Add(0, 0);
            list1.Add(1, 1);
            list1.Add(2, 2);
            IEnumerable ilist = (IEnumerable)list1;

            var idx = 0;
            foreach (var item in ilist)
            {
                var kvp = (KeyValuePair<int, int>)item;
                Assert.Equal(kvp, list1[idx]);
                idx++;
            }
        }

        [Fact]
        public void InsertsItemsInOrder()
        {
            var list1 = new SortedDuplicatesList<int, int>();
            list1.Add(0, 0);
            list1.Add(2, 2);
            list1.Add(1, 1);
            list1.Add(4, 4);
            list1.Add(3, 3);

            Assert.Equal(0, list1.Items[0].Key);
            Assert.Equal(1, list1.Items[1].Key);
            Assert.Equal(2, list1.Items[2].Key);
            Assert.Equal(3, list1.Items[3].Key);
            Assert.Equal(4, list1.Items[4].Key);

            var list2 = new SortedDuplicatesList<string, int>();
            list2.Add("b", 0);
            list2.Add("d", 2);
            list2.Add("a", 1);
            list2.Add("c", 4);

            Assert.Equal("a", list2.Items[0].Key);
            Assert.Equal("b", list2.Items[1].Key);
            Assert.Equal("c", list2.Items[2].Key);
            Assert.Equal("d", list2.Items[3].Key);

            var list3 = new SortedDuplicatesList<DateTime, int>();
            var jan = new DateTime(4015, 03, 01);
            var feb = new DateTime(4015, 03, 01);
            var mar = new DateTime(4015, 03, 01);
            var apr = new DateTime(4015, 03, 01);

            list3.Add(feb, 4);
            list3.Add(jan, 4);
            list3.Add(apr, 4);
            list3.Add(mar, 4);

            Assert.Equal(jan, list3.Items[0].Key);
            Assert.Equal(feb, list3.Items[1].Key);
            Assert.Equal(mar, list3.Items[2].Key);
            Assert.Equal(apr, list3.Items[3].Key);
        }


        [Fact]
        public void AddsDuplicateKeys()
        {
            var list = new SortedDuplicatesList<string, int>();
            list.Add("a", 1);
            list.Add("a", 2);

            Assert.Equal(2, list.Items.Count);

            Assert.Single(list.Items.Where(x => x.Value == 1));
            Assert.Single(list.Items.Where(x => x.Value == 2));
        }

        [Fact]
        public void RemovesCorrectItem()
        {
            var list = new SortedDuplicatesList<string, int>();

            list.Add("a", 1);
            list.Add("a", 2);
            list.Add("a", 3);

            list.Remove("a", 2);
            Assert.Single(list.Items.Where(x => x.Value == 1));
            Assert.Empty(list.Items.Where(x => x.Value == 2));
            Assert.Single(list.Items.Where(x => x.Value == 3));
        }

        [Fact]
        public void IteratesOverEveryItem()
        {
            var list = new SortedDuplicatesList<string, int>();

            list.Add("a", 1);
            list.Add("a", 2);
            list.Add("a", 3);
            var possibleValues = new List<int>() { 1, 2, 3 };
            foreach (var kvp in list)
            {
                Assert.Contains(kvp.Value, possibleValues);
            }
        }
    }
}
