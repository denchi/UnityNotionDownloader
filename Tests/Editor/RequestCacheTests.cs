using NUnit.Framework;
using Game.Serialization.Notion;
using UnityEngine;

public class RequestCacheTests
{
    [SetUp]
    public void Setup()
    {
        PlayerPrefs.DeleteAll();
    }

    [Test]
    public void CacheDatabase_SavesAndRetrievesValue()
    {
        RequestCache.CacheDatabase("db", "value");
        var success = RequestCache.GetCachedDatabase("db", out var cached);
        Assert.IsTrue(success);
        Assert.AreEqual("value", cached);
    }

    [Test]
    public void LastEditedTime_SetAndGet_ReturnsValue()
    {
        RequestCache.SetLastEditedTime("db", "code");
        var result = RequestCache.GetLastEditedTime("db");
        Assert.AreEqual("code", result);
    }
}
