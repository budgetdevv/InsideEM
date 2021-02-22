﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InsideEM.Enumerators;
using InsideEM.EqualityComparers;
using InsideEM.Memory;

namespace InsideEM.Collections
{
    public struct InsideStack<T, MemoryT> where MemoryT: 
        struct, IInsideMemory<T>
    {
        static InsideStack()
        {
            DefEqualityComp = new InsideDefaultEqualityComparer<T>();
        }
        
        private static InsideDefaultEqualityComparer<T> DefEqualityComp;
        
        //TODO: Test for possible regression in performance due to inlining
        
        private const MethodImplOptions Opt = MethodImplOptions.AggressiveInlining;
        
        public int Count
        {
            [MethodImpl(Opt)]
            get => unchecked(ReadIndex + 1);
        }

        private int ReadIndex;

        private MemoryT Memory;

        public T this[int Index]
        {
            [MethodImpl(Opt)]
            get => Memory.Arr[Index];

            [MethodImpl(Opt)]
            set => Memory.Arr[Index] = value;
        }
        
        [MethodImpl(Opt)]
        public InsideStack(ref MemoryT memory)
        {
            Memory = memory;

            ReadIndex = -1;
        }

        //[MethodImpl(Opt)] Pointless to inline a slow path anyway...
        public InsideStack(int InitCapacity, IInsideMemoryAllocator<T, MemoryT> Allocator)
        {
            Allocator.Allocate(InitCapacity, out Memory);

            ReadIndex = -1;
        }

        [MethodImpl(Opt)]
        public void Enqueue<AllocatorT>(ref T Item, ref AllocatorT Allocator) 
            where AllocatorT: struct, IInsideMemoryAllocator<T, MemoryT>
        {
            var Arr = Memory.Arr;
            
            if ((uint) unchecked(++ReadIndex) >= Arr.Length)
            {
                Resize(ref Allocator);
            }

            Arr[ReadIndex] = Item;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize<AllocatorT>(ref AllocatorT Allocator) 
            where AllocatorT: struct, IInsideMemoryAllocator<T, MemoryT>
        {
            Allocator.ResizeNext(ref Memory);
        }
        
        [MethodImpl(Opt)]
        public void Dequeue(out T Item)
        {
            if (ReadIndex == -1)
            {
                Unsafe.SkipInit(out Item);

                return;
            }
            
            Item = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Memory.Arr), unchecked(ReadIndex--));
        }
        
        [MethodImpl(Opt)]
        public ref T DequeueAsRef()
        {
            if (ReadIndex == -1)
            {
                return ref Unsafe.NullRef<T>();
            }
            
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Memory.Arr), unchecked(ReadIndex--));
        }

        [MethodImpl(Opt)]
        public Span<T> AsSpan()
        {
            return Memory.Arr.AsSpan(0, Count);
        }
        
        [MethodImpl(Opt)]
        public Span<T> AsSpan(int StartIndex, int count)
        {
            return Memory.Arr.AsSpan(StartIndex, count);
        }

        [MethodImpl(Opt)]
        public void Clear()
        {
            ReadIndex = -1;
        }

        [MethodImpl(Opt)]
        public RefEnumerator<T> GetEnumerator()
        {
            var Span = Memory.Arr.AsSpan(0, Count);
            
            return new RefEnumerator<T>(ref Span);
        }
        
        [MethodImpl(Opt)]
        public RefEnumerator<T> GetEnumerator(int StartingIndex, int count)
        {
            var Span = Memory.Arr.AsSpan(StartingIndex, count);
            
            return new RefEnumerator<T>(ref Span);
        }
        
        [MethodImpl(Opt)]
        public void Dispose<AllocatorT>(ref AllocatorT Allocator) 
            where AllocatorT: struct, IInsideMemoryAllocator<T, MemoryT>
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) //Release stuff for GC to cleanup
            {
                Memory.Arr.AsSpan().Fill(default);
            }
            
            Allocator.Recycle(ref Memory);
        }
    }
}