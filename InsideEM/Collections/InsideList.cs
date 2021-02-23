using System;
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
        //General Comparison: https://shorturl.at/akmDQ
        
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

        internal MemoryT Memory;

        public T this[int Index]
        {
            //The bound checks benefit this array in 2 ways: 
            //  - We don't have to do if (Index < 0) https://stackoverflow.com/questions/29343533/is-it-more-efficient-to-perform-a-range-check-by-casting-to-uint-instead-of-chec
            //  - We throw an exception if user tries to read / write stuff that doesn't exist in the array; this is paramount as Dispose() / Resize() gets rid of GC references
            //via .AsSpan(0, Count).Fill(default). Consequentially, stuff written outside would not be cleared!

            //Unchecked gets rid of overflow checking for cast to uint
            
            [MethodImpl(Opt)]
            get
            {
                if (unchecked((uint)Index) > unchecked((uint)ReadIndex))
                {
                    throw new IndexOutOfRangeException();
                }
                
                return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Memory.Arr), Index);
            }

            [MethodImpl(Opt)]
            set
            {
                if (unchecked((uint)Index) > unchecked((uint)ReadIndex))
                {
                    throw new IndexOutOfRangeException();
                }

                Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Memory.Arr), Index) = value;
            }
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
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Memory.Arr), ReadIndex);
        }

        [MethodImpl(Opt)]
        public void Add<AllocatorT>(ref T Item, ref AllocatorT Allocator) 
            where AllocatorT: struct, IInsideMemoryAllocator<T, MemoryT>
        {
            var Arr = Memory.Arr;

            unchecked
            {
                var NewReadIndex = ++ReadIndex;
            
                if ((uint)NewReadIndex >= (uint)Arr.Length)
                {
                    Resize(ref Allocator, out Arr);
                }

                Arr[NewReadIndex] = Item;
            }
        }
        
        [MethodImpl(Opt)]
        public void AddMany<AllocatorT>(int StartingIndex, int count, ref InsideArray<T, MemoryT> Arr, ref AllocatorT Allocator) 
            where AllocatorT: struct, IInsideMemoryAllocator<T, MemoryT>
        {
            AddMany(StartingIndex, count, ref Arr.Memory, ref Allocator);
        }
        
        [MethodImpl(Opt)]
        public void AddMany<AllocatorT>(int StartingIndex, int count, ref MemoryT memory, ref AllocatorT Allocator) 
            where AllocatorT: struct, IInsideMemoryAllocator<T, MemoryT>
        {
            var Span = memory.Arr.AsSpan(StartingIndex, count);
            
            AddMany(ref Span, ref Allocator);
        }

        [MethodImpl(Opt)]
        public void AddMany<AllocatorT>(ref Span<T> Span, ref AllocatorT Allocator) 
            where AllocatorT: struct, IInsideMemoryAllocator<T, MemoryT>
        {
            var OldReadIndex = ReadIndex;
            
            unchecked
            {
                ReadIndex += Span.Length;
            }

            var Arr = Memory.Arr;

            if (Count > Arr.Length)
            {
                //TODO: Review if Count * 2 is optimal
                
                ResizeAtLeast(unchecked(Count * 2), ref Allocator, out Arr);
            }

            var SourceSpan = Arr.AsSpan();
            
            SourceSpan.CopyTo(Arr.AsSpan(OldReadIndex));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize<AllocatorT>(ref AllocatorT Allocator, out T[] Arr) 
            where AllocatorT: struct, IInsideMemoryAllocator<T, MemoryT>
        {
            var Span = Memory.Arr.AsSpan(0, Count);

            Allocator.ResizeNext(ref Memory, ref Span);

            Arr = Memory.Arr;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizeAtLeast<AllocatorT>(int Size, ref AllocatorT Allocator, out T[] Arr) 
            where AllocatorT: struct, IInsideMemoryAllocator<T, MemoryT>
        {
            var Span = Memory.Arr.AsSpan(0, Count);
            
            Allocator.ResizeNext(ref Memory, ref Span);

            Arr = Memory.Arr;
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
                Memory.Arr.AsSpan(0, Count).Fill(default);
            }
            
            Allocator.Recycle(ref Memory);
        }
    }
}
