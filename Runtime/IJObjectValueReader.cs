using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Game.Serialization
{
    /// <summary>
    /// Retrieves a value from a json data
    /// </summary>
    public interface IJObjectValueReader
    {
        float GetFloat(JObject props, string key, float defaultValue);

        IEnumerable<string> GetRelations(JObject props, string key);

        T GetEnum<T>(JObject props, string key, T defaultValue) where T : struct;
        T GetFlags<T>(JObject props, string key, T defaultValue) where T : Enum;

        bool GetBool(JObject props, string key, bool defaultValue);

        int GetInt(JObject props, string key, int defaultValue);

        string GetString(JObject props, string key, string defaultValue);
    }
}