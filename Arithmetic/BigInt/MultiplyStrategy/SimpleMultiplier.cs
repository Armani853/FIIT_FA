using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        uint[] resultDigits = MultiplyDigits(a.GetDigits(), b.GetDigits());
        bool isNegative = a.IsNegative ^ b.IsNegative;
        return new BetterBigInteger(resultDigits, isNegative);
    }

    private static uint[] MultiplyDigits(ReadOnlySpan<uint> leftDigits, ReadOnlySpan<uint> rightDigits)
    {
        if (IsZero(leftDigits) || IsZero(rightDigits))
        {
            return [0];
        }

        ulong[] accumulator = new ulong[leftDigits.Length + rightDigits.Length];

        for (int i = 0; i < leftDigits.Length; i++)
        {
            ulong carry = 0;
            ulong left = leftDigits[i];

            for (int j = 0; j < rightDigits.Length; j++)
            {
                ulong total = accumulator[i + j] + (left * rightDigits[j]) + carry;
                accumulator[i + j] = (uint)total;
                carry = total >> 32;
            }

            int carryIndex = i + rightDigits.Length;
            while (carry > 0)
            {
                ulong total = accumulator[carryIndex] + carry;
                accumulator[carryIndex] = (uint)total;
                carry = total >> 32;
                carryIndex++;
            }
        }

        int last = accumulator.Length - 1;
        while (last > 0 && accumulator[last] == 0)
        {
            last--;
        }

        uint[] result = new uint[last + 1];
        for (int i = 0; i <= last; i++)
        {
            result[i] = (uint)accumulator[i];
        }

        return result;
    }

    private static bool IsZero(ReadOnlySpan<uint> digits)
    {
        foreach (uint digit in digits)
        {
            if (digit != 0)
            {
                return false;
            }
        }

        return true;
    }
}
