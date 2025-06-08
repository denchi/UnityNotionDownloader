using UnityEngine;

namespace Game.Serialization.Notion
{
    public static class RequestCache
    {
        public static void CacheDatabase(string databaseId, string databaseValue)
        {
            PlayerPrefs.SetString($"cached_{databaseId}_value", databaseValue);
        }
        
        public static bool GetCachedDatabase(string databaseId, out string cachedValue)
        {
            if (PlayerPrefs.HasKey($"cached_{databaseId}_value"))
            {
                cachedValue = PlayerPrefs.GetString($"cached_{databaseId}_value");
                return true;
            }

            cachedValue = null;
            return false;
        }
        
        public static string GetLastEditedTime(string databaseId)
        {
            return PlayerPrefs.HasKey($"cached_{databaseId}_code") ? PlayerPrefs.GetString($"cached_{databaseId}_code") : null;
        }

        public static void SetLastEditedTime(string databaseId, string lastEditedTime)
        {
            PlayerPrefs.SetString($"cached_{databaseId}_code", lastEditedTime);
        }
    }
}