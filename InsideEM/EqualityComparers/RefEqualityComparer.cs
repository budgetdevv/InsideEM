using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace InsideEM.EqualityComparers
{
    public readonly struct RefDefaultEqualityComparer<T>: IInsideEqualityComparer<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ref T First, ref T Second)
        {
            return Unsafe.AreSame(ref First, ref Second);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(ref T Item)
        {
            return EqualityComparer<T>.Default.GetHashCode(Item);
        }
    }
}