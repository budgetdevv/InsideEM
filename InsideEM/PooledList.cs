using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InsideEM
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

        public PooledList(int InitCapacity)
        {
            Arr = ArrayPool<T>.Shared.Rent(InitCapacity);

            ReadIndex = -1;
        }

        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public void Add(ref T Item)
        {
            Add(Item);
        }
        
        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public void Add(T Item)
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
        public bool Remove(ref T Item)
        {
            return Remove(Item);
        }
        
        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public bool Remove(T Item)
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

                Unsafe.Subtract(ref CurrentElemRef, 1);
            }

            return false;
            
            RemoveSlow:
            {
                //The total count that needs to be moved would be Diff
                
                //E.x. 0, 1, 2, 3, 4, 5 , 6, 7
                
                //Say 4 is deleted... 7 - 4 = 3
                
                //We take the element next to CurrentElem, which is guaranteed to exist
                //should CurrentElem != LastElem

                var DestSpan = MemoryMarshal.CreateSpan(ref CurrentElemRef, Diff);
                
                Unsafe.Add(ref CurrentElemRef, 1);

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
        
        public struct Enumerator//: IEnumerator<T>
        {
            private T[] Arr;
            
            private uint CurrentIndex;

            public Enumerator(T[] arr)
            {
                Arr = arr;

                CurrentIndex = 0;
                
                Unsafe.SkipInit(out _Current);
            }
            
            public T Current
            {
                [MethodImpl(EMHelpers.InlineAndOptimize)]
                get => _Current;
            }

            private T _Current;
            
            public bool MoveNext()
            {
                if (CurrentIndex >= Arr.Length)
                {
                    return false;
                }

                _Current = Arr[CurrentIndex];

                unchecked
                {
                    CurrentIndex++;
                }
                
                return true;
            }

            public void Reset()
            {
                CurrentIndex = 0;
            }
        } 

        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(Arr);
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