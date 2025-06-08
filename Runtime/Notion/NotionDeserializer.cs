using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Serialization.Notion
{
    public abstract class NotionDeserializer<T> : INotionDeserializer, IDataDeserializer<T>, IDataDeserializerEvents
    {
        public NotionDownloader notionDownloader { get; set; }
        public IJObjectValueReader valueReader { get; set; }
        public DatabaseCache databaseCache { get; set; }
        
        //
        
        public object CreateObject(JObject props) => Create(props);
        public abstract T Create(JObject props);

        public virtual T Find(T remote) { return remote; }
        public virtual void Save(T remote) { }
        
        //
        
        public virtual void OnFinished(IList list) { }

        protected T TryUpdateOrCreateCachedVersion(T remote)// where T : IItemWithId
        {
            Debug.Assert(remote != null, "The remote asset is NULL!");
            
#if UNITY_EDITOR
            var json = EditorJsonUtility.ToJson(remote);
            
            var local = Find(remote);
            if (local != null)
            {
                // Update Name
                EditorJsonUtility.FromJsonOverwrite(json, local);

                Debug.LogWarning($"[UnitDeserializer] Updated Local Version of {local}");
                
                return local;
            }
            
            // Create
            Debug.LogWarning($"[UnitDeserializer] No Local Version of {remote} found. Creating a new one...");

            // Duplicate Unit
            local = JsonUtility.FromJson<T>(json);
            
            // Update UnityDataContainer Units
            Save(local);

            return local;
#else
            return remote;
#endif
        }
        
        protected T TryCreateCachedVersion(T remote)
        {
            Debug.Assert(remote != null, "The remote asset is NULL!");
            
#if UNITY_EDITOR
            var json = EditorJsonUtility.ToJson(remote);
            
            // Duplicate Unit
            var local = JsonUtility.FromJson<T>(json);
            
            // Update UnityDataContainer Units
            Save(local);

            return local;
#else
            return remote;
#endif
        }
    }
}