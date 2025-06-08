using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Serialization.Notion
{
    public class NotionValueReader : IJObjectValueReader
    {
        public float GetFloat(JObject props, string key, float defaultValue)
        {
            try
            {
                return (float)props[key]["number"];
            }
            catch (Exception ex)
            {
                return defaultValue;
            }
        }

        public IEnumerable<string> GetRelations(JObject props, string key)
        {
            try
            {
                var jo = props[key];
                var jr = jo["relation"];
                if (jr is JArray jarr)
                {
                    return jarr.Select(r => (string)r["id"]).ToList();   
                }
                    
                return new List<string>{ (string)jr["id"] }; 
            }
            catch (Exception ex)
            {
                return new List<string>();
            }
        }

        public T GetEnum<T>(JObject props, string key, T defaultValue) where T: struct
        {
            try
            {
                var value = (string)props[key]["select"]?["name"];
                if (value == null)
                    throw new Exception("The value can not be null!");

                value = value.Replace(" ", "");

                if (!Enum.TryParse(typeof(T), value, true, out var result))
                    throw new Exception($"Could not read value '{value}'");
                
                return (T)result;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error reading '{key}'. {e.Message}");
                return defaultValue;
            }
        }
        
        public T GetFlags<T>(JObject props, string key, T defaultValue) where T: Enum
        {
            try
            {
                T multiSelectValue = defaultValue;
                {
                    var multiSelect = props[key]["multi_select"];
                    if (multiSelect == null)
                        return defaultValue;
                    
                    var enumValues = multiSelect.Select(obj =>
                    {
                        var value = (string)obj?["name"];
                        if (value == null)
                            throw new Exception("The value can not be null!");

                        value = value.Replace(" ", "");

                        if (!Enum.TryParse(typeof(T), value, true, out var result))
                            throw new Exception($"Could not read value '{value}'");

                        return (T)result;
                    }).ToList();

                    if (enumValues.Count == 0)
                        return defaultValue;

                    foreach (var enumValue in enumValues)
                    {
                        multiSelectValue = (T)Enum.ToObject(typeof(T), Convert.ToInt32(multiSelectValue) | Convert.ToInt32(enumValue));
                    }
                }
                return multiSelectValue;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error reading '{key}'. {e.Message}");
                return defaultValue;
            }
        }

        public bool GetBool(JObject props, string key, bool defaultValue)
        {
            try
            {
                return (bool)props[key]["checkbox"];
            }
            catch (Exception ex)
            {
                return defaultValue;
            }
        }

        public int GetInt(JObject props, string key, int defaultValue)
        {
            try
            {
                var jo = props[key] as JObject;
                if (jo.ContainsKey("number"))
                {
                    return (int)props[key]["number"]; 
                }
                
                if (jo.ContainsKey("unique_id"))
                {
                    return (int)props[key]["unique_id"]["number"]; 
                }
            }
            catch (Exception ex)
            {
                //
            }
            
            return defaultValue;
        }

        public string GetString(JObject props, string key, string defaultValue)
        {
            try
            {
                var type = (string)props[key]["type"];
                switch (type)
                {
                    case "title": return GetPlainText(props[key]["title"] as JArray);
                    case "rich_text": return GetPlainText(props[key]["rich_text"] as JArray);
                    case "select": return (string)props[key]["select"]?["name"];
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                return defaultValue;
            }
            
            String GetPlainText(JArray prop)
            {
                try
                {
                    var jo = prop[0];
                    var js = jo["plain_text"];
                    return (string) js;
                }
                catch (Exception ex)
                {
                    return defaultValue;
                }
            }
        }
    }
}