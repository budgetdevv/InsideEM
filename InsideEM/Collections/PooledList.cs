using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InsideEM.Collections
{
    public struct PooledList<T>: IDisposable
    {
        public int Count
        {
            get => unchecked(ReadIndex + 1);
        }

        private int ReadIndex;

        private T[] Arr;

        public T this[int Index]
        {
            get
            {
                return Arr[Index];
            }

            set
            {
                Arr[Index] = value;
            }
        }

        public PooledList(int InitCapacity)
        {
            Arr = ArrayPool<T>.Shared.Rent(InitCapacity);

            ReadIndex = -1;
        }
        
        public ref T GetByRef(int Index)
        {
            return ref Arr[Index];
        }

        public void Add(T Item)
        {
            Add(ref Item);
        }
        
        public void Add(ref T Item)
        {
            if ((uint) unchecked(++ReadIndex) >= Arr.Length)
            {
                Resize();
            }

            Arr[ReadIndex] = Item;
        }
        
        private void Resize()
        {
            var OldArr = Arr;
            
            var OldSpan = OldArr.AsSpan();

            Arr = ArrayPool<T>.Shared.Rent(Arr.Length * 2);
            
            OldSpan.CopyTo(Arr);
            
            ArrayPool<T>.Shared.Return(OldArr);
        }

        public bool Remove(T Item)
        {
            return Remove(ref Item);
        }
        
        public unsafe bool Remove(ref T Item)
        {
            if (ReadIndex == 0)
            {
                return false;
            }

            ref var FirstElemRef = ref Arr[0];

            ref var LastElemRef = ref Arr[ReadIndex];

            ref var CurrentElemRef = ref LastElemRef;
            
            int Diff = 0;
            
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

        public void AsSpan(out Span<T> Span)
        {
            Span = Arr.AsSpan(0, Count);
        }
        
        public void AsSpan(int StartIndex, int count, out Span<T> Span)
        {
            Span = Arr.AsSpan(StartIndex, count);
        }

        public void Clear()
        {
            ReadIndex = -1;
        }
        
        public ref struct RefEnumerator
        {
            private Span<T> Span;

            private int CurrentIndex;
            
            public RefEnumerator(ref PooledList<T> List)
            {
                List.AsSpan(out Span);

                CurrentIndex = -1;
            }
            
            public ref T Current
            {
                get => ref Span[CurrentIndex];
            }

            public bool MoveNext()
            {
                return unchecked((uint)++CurrentIndex) < Span.Length;
            }

            public void Reset()
            {
                CurrentIndex = -1;
            }
        } 

        public RefEnumerator GetEnumerator()
        {
            return new RefEnumerator(ref this);
        }
        
        public void Dispose()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) //Release stuff for GC to cleanup
            {
                Arr.AsSpan().Fill(default);
            }
            
            ArrayPool<T>.Shared.Return(Arr);
        }
    }
}
