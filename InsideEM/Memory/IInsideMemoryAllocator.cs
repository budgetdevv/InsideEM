using System;
using System.Runtime.CompilerServices;

namespace InsideEM.Memory
{
    public interface IInsideMemoryAllocator<T, MemoryT>: IDisposable where MemoryT: struct, IInsideMemory<T>
    {
        public void Allocate(int Size, out MemoryT Memory);
        
        public void Recycle(ref MemoryT Memory);
        
        public void Resize(int NewSize, ref MemoryT Memory);
        
        public void ResizeNext(ref MemoryT Memory);
    }
}