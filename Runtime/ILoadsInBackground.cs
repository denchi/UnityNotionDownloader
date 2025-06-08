namespace Game.Serialization
{
    /// <summary>
    /// An interface for objects that can be loaded in the background.
    /// </summary>
    public interface ILoadsInBackground
    {
        void StartLoading();
        void UpdateLoading(float ratio);
        void EndLoading();
    }
}