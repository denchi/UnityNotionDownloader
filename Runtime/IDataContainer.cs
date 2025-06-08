using System.Collections.Generic;

namespace Game.Serialization
{
	/// <summary>
	/// Data loader interface.
	/// </summary>
	public interface IDataContainer
	{
		void Load(ILoadsInBackground notifier);
	}
}

