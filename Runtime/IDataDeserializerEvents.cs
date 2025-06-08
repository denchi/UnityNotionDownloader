using System.Collections;

namespace Game.Serialization
{
    public interface IDataDeserializerEvents
    {
        void OnFinished(IList list);
    }
}