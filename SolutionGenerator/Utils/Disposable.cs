using System;
using System.Linq;

namespace SolutionGen.Utils
{
    public class Disposable : IDisposable
    {
        private readonly IDisposable[] disposables;

        public Disposable(params IDisposable[] disposables)
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