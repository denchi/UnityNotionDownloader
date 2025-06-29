using NUnit.Framework;
using System.Collections.Generic;
using Game.Serialization;

public class DatabaseCacheExtraTests
{
    [Test]
    public void ContainsDatabase_ReturnsCorrectValue()
    {
        var cache = new DatabaseCache();
        cache.SetTableFromCache<string>("db", new List<object>{"a"});
        Assert.IsTrue(cache.ContainsDatabase("db"));
        Assert.IsFalse(cache.ContainsDatabase("missing"));
    }

    [Test]
    public void GetItemsFromCache_FiltersItems()
    {
        var cache = new DatabaseCache();
        var list = new List<object>{ "one", "two", "three" };
        cache.SetTableFromCache<string>("db", list);
        var result = cache.GetItemsFromCache<string>(s => s.Contains("o"));
        CollectionAssert.AreEqual(new List<string>{"one","two"}, result);
    }
}
