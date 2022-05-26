using System;
using System.Threading.Tasks;

namespace CustomResources.Utils.Concepts
{
	public interface IAsyncInitialized
	{
		public Task InitializeAsync();
	}
}

