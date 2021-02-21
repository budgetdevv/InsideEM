using System.Runtime.CompilerServices;

namespace InsideEM.EmbedMenu
{
    public static class EMHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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