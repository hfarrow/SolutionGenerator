using System;
using System.Linq;

namespace SolutionGen.Utils
{
    public class CompositeDisposable : IDisposable
    {
        private readonly IDisposable[] disposables;

        public CompositeDisposable(params IDisposable[] disposables)
        {
            this.disposables = disposables;
        }

        public void Dispose()
        {
            foreach (IDisposable disposable in disposables.Reverse())
            {
                disposable.Dispose();
            }
        }
    }
}