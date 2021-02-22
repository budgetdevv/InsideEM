using System;
using System.Runtime.CompilerServices;
using InsideEM.Collections;

namespace InsideEM.Enumerators
{
    public ref struct RefEnumerator<T>
    {
        private readonly Span<T> Span;

        private int CurrentIndex;
            
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefEnumerator(ref Span<T> span)
        {
            Span = span;
            
            CurrentIndex = -1;
        }

        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Span[CurrentIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return unchecked((uint)++CurrentIndex) < Span.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            CurrentIndex = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefEnumerator<T> GetEnumerator()
        {
            return this;
        }
    } 
}