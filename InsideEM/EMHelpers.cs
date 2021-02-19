using System.Runtime.CompilerServices;

namespace InsideEM
{
    public static class EMHelpers
    {
        public const MethodImplOptions InlineAndOptimize =
            MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
        
        [MethodImpl(InlineAndOptimize)]
        public static int DivideAndRoundUpFast(int Num, int Divisor)
        {
            unchecked
            {
                var Remainder = Num % Divisor;

                if (Remainder == 0)
                {
                    return Num / Divisor;
                }

                return ((Num - Remainder) / Divisor) + 1;
            }
        }
    }
    
}