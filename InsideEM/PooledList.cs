﻿using System;
using System.Buffers;
using System.Runtime.CompilerServices;

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
        public void Remove(ref T Item)
        {
            Remove(Item);
        }
        
        [MethodImpl(EMHelpers.InlineAndOptimize)]
        public void Remove(T Item)
        {
            unchecked
            {
                ReadIndex--;
            }
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