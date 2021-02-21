using System;
using System.Runtime.CompilerServices;

namespace InsideEM.Memory
{
    public interface IInsideMemory<T>: IDisposable
    {
        public T[] Arr { get; }
        
        public void CopyTo<MemoryT>(MemoryT DestMem) where MemoryT : IInsideMemory<T>;
    }
}