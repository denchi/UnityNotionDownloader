using NUnit.Framework;
using System.Collections.Generic;
using Game.Serialization.Notion;
using Newtonsoft.Json.Linq;

public class NotionValueReaderTests
{
    private NotionValueReader _reader;

    [SetUp]
    public void Setup()
    {
        _reader = new NotionValueReader();
    }

    [Test]
    public void GetFloat_ReturnsValue()
    {
        var jo = JObject.Parse("{ 'number': 1.5 }");
        var props = new JObject { { "key", jo } };
        var result = _reader.GetFloat(props, "key", 0f);
        Assert.AreEqual(1.5f, result);
    }

    [Test]
    public void GetFloat_Invalid_ReturnsDefault()
    {
        var props = new JObject();
        var result = _reader.GetFloat(props, "missing", 2f);
        Assert.AreEqual(2f, result);
    }

    [Test]
    public void GetRelations_ReturnsList()
    {
        var arr = new JArray { new JObject { { "id", "a" } }, new JObject { { "id", "b" } } };
        var jo = new JObject { { "relation", arr } };
        var props = new JObject { { "key", jo } };
        var result = _reader.GetRelations(props, "key");
        CollectionAssert.AreEqual(new List<string>{"a","b"}, result);
    }

    enum TestEnum { A, B }
    [Test]
    public void GetEnum_ReturnsParsedValue()
    {
        var select = new JObject { { "name", "A" } };
        var jo = new JObject { { "select", select } };
        var props = new JObject { { "key", jo } };
        var result = _reader.GetEnum(props, "key", TestEnum.B);
        Assert.AreEqual(TestEnum.A, result);
    }

    [System.Flags]
    enum FlagEnum { None=0, A=1, B=2 }
    [Test]
    public void GetFlags_ReturnsCombinedFlags()
    {
        var ms = new JArray { new JObject { { "name", "A" } }, new JObject { { "name", "B" } } };
        var jo = new JObject { { "multi_select", ms } };
        var props = new JObject { { "key", jo } };
        var result = _reader.GetFlags(props, "key", FlagEnum.None);
        Assert.AreEqual(FlagEnum.A | FlagEnum.B, result);
    }

    [Test]
    public void GetBool_ReturnsValue()
    {
        var jo = new JObject { { "checkbox", true } };
        var props = new JObject { { "key", jo } };
        var result = _reader.GetBool(props, "key", false);
        Assert.IsTrue(result);
    }

    [Test]
    public void GetInt_ReturnsNumber()
    {
        var jo = new JObject { { "number", 3 } };
        var props = new JObject { { "key", jo } };
        var result = _reader.GetInt(props, "key", 0);
        Assert.AreEqual(3, result);
    }

    [Test]
    public void GetString_Title_ReturnsPlainText()
    {
        var title = new JArray { new JObject { { "plain_text", "txt" } } };
        var jo = new JObject { { "type", "title" }, { "title", title } };
        var props = new JObject { { "key", jo } };
        var result = _reader.GetString(props, "key", "def");
        Assert.AreEqual("txt", result);
    }
}
