using System.Runtime.CompilerServices;

namespace InsideEM.EqualityComparers
{
    public interface IInsideEqualityComparer<T>
    {
        public bool Equals(ref T First, ref T Second);
        
        public int GetHashCode(ref T Item);
    }
}