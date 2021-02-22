using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InsideEM.Enumerators;
using InsideEM.EqualityComparers;
using InsideEM.Memory;

namespace InsideEM.Collections
{
    public struct InsideArray<T, MemoryT> where MemoryT :
        struct, IInsideMemory<T>
    {
        //TODO: Test for possible regression in performance due to inlining
        
        private const MethodImplOptions Opt = MethodImplOptions.AggressiveInlining;
        
        private MemoryT Memory;

        [MethodImpl(Opt)]
        public InsideArray(ref MemoryT memory)
        {
            Memory = memory;
        }
        
        public InsideArray(int InitSize, IInsideMemoryAllocator<T, MemoryT> Allocator)
        {
            Allocator.Allocate(InitSize, out Memory);
        }
        
        public T this[int Index]
        {
            [MethodImpl(Opt)]
            get => Memory.Arr[Index];

            [MethodImpl(Opt)]
            set => Memory.Arr[Index] = value;
        }

        public int Length
        {
            [MethodImpl(Opt)]
            get => Memory.Arr.Length;
        }
        
        public void Resize<AllocatorT>(int NewSize, ref AllocatorT Allocator)
            where AllocatorT: struct, IInsideMemoryAllocator<T, MemoryT>
        {
            Allocator.Resize(NewSize, ref Memory);
        }
        
        [MethodImpl(Opt)]
        public void Dispose<AllocatorT>(ref AllocatorT Allocator)
            where AllocatorT: struct, IInsideMemoryAllocator<T, MemoryT>
        {
            Allocator.Recycle(ref Memory);
        }
    }
}
