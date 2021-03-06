﻿using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace InsideEM.Memory.Allocators
{
    public readonly struct ArrayPoolMemory<T> : IInsideMemory<T>
    {
        private const MethodImplOptions Opt = MethodImplOptions.AggressiveInlining;
        
        // ReSharper disable once ConvertToAutoProperty
        public T[] Arr
        {
            [MethodImpl(Opt)]
            get => arr;
        }

        private readonly T[] arr;

        [MethodImpl(Opt)]
        internal ArrayPoolMemory(T[] _Arr)
        {
            arr = _Arr;
        }
        
        [MethodImpl(Opt)]
        public void CopyTo<MemoryT>(MemoryT DestMem) where MemoryT : IInsideMemory<T>
        {
            Arr.AsSpan().CopyTo(DestMem.Arr);
        }
        
        [MethodImpl(Opt)]
        public void Dispose()
        {
            //Nothing here xd
        }
    }
    
    public struct ArrayPoolAllocator<T>: IInsideMemoryAllocator<T, ArrayPoolMemory<T>>
    {
        private const MethodImplOptions Opt = MethodImplOptions.AggressiveInlining;
        
        [MethodImpl(Opt)]
        public void Allocate(int Size, out ArrayPoolMemory<T> Memory)
        {
            Memory = new ArrayPoolMemory<T>(ArrayPool<T>.Shared.Rent(Size));
        }

        [MethodImpl(Opt)]
        public void Recycle(ref ArrayPoolMemory<T> Memory)
        {
            ArrayPool<T>.Shared.Return(Memory.Arr);
        }

        [MethodImpl(Opt)]
        public void Resize(int NewSize, ref ArrayPoolMemory<T> Memory)
        {
            ref var OldMem = ref Memory;

            var OldSpan = OldMem.Arr.AsSpan();
            
            Allocate(NewSize, out Memory);
            
            OldSpan.CopyTo(Memory.Arr);
            
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                OldSpan.Fill(default);
            }

            Recycle(ref OldMem);
        }
        
        [MethodImpl(Opt)]
        public void Resize(int NewSize, ref ArrayPoolMemory<T> Memory, ref Span<T> Span)
        {
            ref var OldMem = ref Memory;
            
            Allocate(NewSize, out Memory);

            var NewSpan = Memory.Arr.AsSpan();
            
            Span.CopyTo(NewSpan);

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Span.Fill(default);
            }

            //Span = NewSpan; //Would have to .Slice() the new span anyway...to copy starting from a specific index

            Recycle(ref OldMem);
        }

        [MethodImpl(Opt)]
        public void ResizeNext(ref ArrayPoolMemory<T> Memory)
        {
            var NewSize = unchecked(Memory.Arr.Length * 2);
            
            Resize(NewSize, ref Memory);
        }

        [MethodImpl(Opt)]
        public void ResizeNext(ref ArrayPoolMemory<T> Memory, ref Span<T> Span)
        {
            var NewSize = unchecked(Memory.Arr.Length * 2);
            
            Resize(NewSize, ref Memory, ref Span);
        }

        [MethodImpl(Opt)]
        public void Dispose()
        {
            //Nothing here xd
        }
    }
}