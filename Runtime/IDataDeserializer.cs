using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Game.Serialization
{
    public interface IDataDeserializer
    {
        object CreateObject(JObject props);
    }

    /// <summary>
    /// Creates an entity based on json data
    /// </summary>
    public interface IDataDeserializer<out T> : IDataDeserializer
    {
        T Create(JObject props);
    }
}