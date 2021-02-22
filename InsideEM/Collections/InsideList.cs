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
    public struct InsideList<T, MemoryT> where MemoryT: 
        struct, IInsideMemory<T>
    {
        static InsideList()
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
        public InsideList(ref MemoryT memory)
        {
            Memory = memory;

            ReadIndex = -1;
        }

        //[MethodImpl(Opt)] Pointless to inline a slow path anyway...
        public InsideList(int InitCapacity, IInsideMemoryAllocator<T, MemoryT> Allocator)
        {
            Allocator.Allocate(InitCapacity, out Memory);

            ReadIndex = -1;
        }

        [MethodImpl(Opt)]
        public ref T GetByRef(int Index)
        {
            return ref Memory.Arr[Index];
        }

        [MethodImpl(Opt)]
        public void Add<AllocatorT>(ref T Item, ref AllocatorT Allocator) 
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
        public bool Remove(ref T Item)
        {
            return Remove(ref Item, ref DefEqualityComp);
        }

        [MethodImpl(Opt)]
        public bool Remove<EqualityComparerT>(ref T Item, ref EqualityComparerT Comp)
            where EqualityComparerT: struct, IInsideEqualityComparer<T>
        {
            if (ReadIndex == 0)
            {
                return false;
            }

            var Arr = Memory.Arr;

            ref var FirstElemRef = ref MemoryMarshal.GetArrayDataReference(Arr);

            ref var LastElemRef = ref Unsafe.Add(ref FirstElemRef, ReadIndex);

            ref var CurrentElemRef = ref LastElemRef;

            while (!Unsafe.IsAddressLessThan(ref CurrentElemRef, ref FirstElemRef))
            {
                if (!Comp.Equals(ref Item, ref CurrentElemRef))
                {
                    CurrentElemRef = ref Unsafe.Subtract(ref CurrentElemRef, 1);

                    continue;
                }
                
                unchecked
                {
                    ReadIndex--;
                }
                    
                var Diff = (int)Unsafe.ByteOffset(ref CurrentElemRef, ref LastElemRef);

                if (Diff == 0) //Fast path, removed elem is last
                {
                    return true;
                }

                //The total count that needs to be moved would be Diff ( Bytes )
                
                //E.x. 0, 1, 2, 3, 4, 5, 6, 7
                
                //Say 4 is deleted... 7 - 4 = 3
                
                //We take the element next to CurrentElem, which is guaranteed to exist
                //should CurrentElem != LastElem

                Diff /= Unsafe.SizeOf<T>();
                
                var DestSpan = MemoryMarshal.CreateSpan(ref CurrentElemRef, Diff);

                CurrentElemRef = ref Unsafe.Add(ref CurrentElemRef, 1);

                var SourceSpan = MemoryMarshal.CreateReadOnlySpan(ref CurrentElemRef, Diff);
                
                SourceSpan.CopyTo(DestSpan);

                return true;
            }

            return false;
        }

        [MethodImpl(Opt)]
        public bool RemoveLast()
        {
            if (ReadIndex == 0)
            {
                return false;
            }
            
            unchecked
            {
                ReadIndex--;
            }

            return true;
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
