using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InsideEM.Collections
{
    public struct PooledList<T>: IDisposable
    {
        private static readonly bool TIsRefType;
        
        static PooledList()
        {
            TIsRefType = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        }
        
        public int Count
        {
            [MethodImpl(EMHelpers.InlineAndOptimize)]
            get => unchecked(ReadIndex + 1);
        }

        private int ReadIndex;

        private T[] Arr;

        public T this[int Index]
        {
            [MethodImpl(EMHelpers.InlineAndOptimize)]
            get
            {
                if ((uint) Index >= Arr.Length)
                {
                    throw new IndexOutOfRangeException($"{Index} is out of range");
                }

                return Arr[Index];
            }

            [MethodImpl(EMHelpers.InlineAndOptimize)]
            set
            {
                if ((uint) Index >= Arr.Length)
                {
                    throw new IndexOutOfRangeException($"{Index} is out of range");
                }

                Arr[Index] = value;
            }
        }

        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public PooledList(int InitCapacity)
        {
            Arr = ArrayPool<T>.Shared.Rent(InitCapacity);

            ReadIndex = -1;
        }

        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public void Add(T Item)
        {
            Add(ref Item);
        }
        
        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public void Add(ref T Item)
        {
            if ((uint) unchecked(++ReadIndex) >= Arr.Length)
            {
                Resize();
            }

            Arr[ReadIndex] = Item;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Resize()
        {
            var OldArr = Arr;
            
            var OldSpan = OldArr.AsSpan();

            Arr = ArrayPool<T>.Shared.Rent(Arr.Length * 2);
            
            OldSpan.CopyTo(Arr);
            
            ArrayPool<T>.Shared.Return(OldArr);
        }
        
        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public bool Remove(T Item)
        {
            return Remove(ref Item);
        }
        
        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public unsafe bool Remove(ref T Item)
        {
            if (ReadIndex == 0)
            {
                return false;
            }

            ref var FirstElemRef = ref Arr[0];

            ref var LastElemRef = ref Arr[ReadIndex];

            ref var CurrentElemRef = ref LastElemRef;
            
            Unsafe.SkipInit(out int Diff);
            
            while (!Unsafe.IsAddressLessThan(ref CurrentElemRef, ref FirstElemRef))
            {
                if (EqualityComparer<T>.Default.Equals(Item, CurrentElemRef))
                {
                    unchecked
                    {
                        ReadIndex--;
                    }

                    if ((Diff = (int) Unsafe.ByteOffset(ref CurrentElemRef, ref LastElemRef)) == 0) //Fast path
                    {
                        return true;
                    }

                    goto RemoveSlow;
                }

                CurrentElemRef = ref Unsafe.Subtract(ref CurrentElemRef, 1);
            }

            return false;
            
            RemoveSlow:
            {
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
        }

        [MethodImpl(EMHelpers.InlineAndOptimize)]
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

        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public void AsSpan(out Span<T> Span)
        {
            Span = Arr.AsSpan(0, Count);
        }
        
        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public void AsSpan(int StartIndex, int count, out Span<T> Span)
        {
            Span = Arr.AsSpan(StartIndex, count);
        }

        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public void Clear()
        {
            ReadIndex = -1;
        }
        
        public ref struct RefEnumerator
        {
            private Span<T> Span;

            private int CurrentIndex;
            
            [MethodImpl(EMHelpers.InlineAndOptimize)]
            public RefEnumerator(ref PooledList<T> List)
            {
                List.AsSpan(out Span);

                CurrentIndex = -1;
            }
            
            public ref T Current
            {
                [MethodImpl(EMHelpers.InlineAndOptimize)]
                get => ref Span[CurrentIndex];
            }

            [MethodImpl(EMHelpers.InlineAndOptimize)]
            public bool MoveNext()
            {
                return unchecked((uint)++CurrentIndex) < Span.Length;
            }

            public void Reset()
            {
                CurrentIndex = -1;
            }
        } 

        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public RefEnumerator GetEnumerator()
        {
            return new RefEnumerator(ref this);
        }
        
        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public void Dispose()
        {
            if (TIsRefType) //Release stuff for GC to cleanup
            {
                Arr.AsSpan().Fill(default);
            }
            
            ArrayPool<T>.Shared.Return(Arr);
        }
    }
}