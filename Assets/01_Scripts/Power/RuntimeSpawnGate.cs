using System;

namespace Octoplug.Power
{
    /// <summary>
    /// Suppresses lifecycle-driven world registration while a production prefab
    /// is being instantiated and configured. Unity invokes Awake/OnEnable during
    /// Instantiate, so runtime creation must open this gate before cloning an
    /// active authored prefab.
    /// </summary>
    public static class RuntimeSpawnGate
    {
        private static int depth;

        public static bool IsStaging => depth > 0;

        public static IDisposable Begin()
        {
            depth++;
            return new Scope();
        }

        private sealed class Scope : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                depth = Math.Max(0, depth - 1);
            }
        }
    }
}
