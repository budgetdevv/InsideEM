using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace InsideEM.EqualityComparers
{
    public readonly struct InsideDefaultEqualityComparer<T>: IInsideEqualityComparer<T>
    {
        private static readonly EqualityComparer<T> EC;

        static InsideDefaultEqualityComparer()
        {
            EC = EqualityComparer<T>.Default;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ref T First, ref T Second)
        {
            return EC.Equals(First, Second);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(ref T Item)
        {
            return EC.GetHashCode(Item);
        }
    }
}