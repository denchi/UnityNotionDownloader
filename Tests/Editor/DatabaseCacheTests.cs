using NUnit.Framework;
using System.Collections.Generic;
using Game.Serialization;

public class DatabaseCacheTests
{
    [Test]
    public void SetAndGetTableFromCache_ReturnsTypedList()
    {
        var cache = new DatabaseCache();
        var list = new List<string> { "a", "b" };
        cache.SetTableFromCache<string>("db", list);
        var result = cache.GetTableFromCache<string>("db");
        Assert.AreEqual(list, result);
    }

    [Test]
    public void AddAndGetItem_ReturnsSameObject()
    {
        var cache = new DatabaseCache();
        var obj = new TestItem();
        cache.AddItemToCache("1", obj);
        var result = cache.GetItemFromCache<TestItem>("1");
        Assert.AreSame(obj, result);
    }

    private class TestItem {}
}
