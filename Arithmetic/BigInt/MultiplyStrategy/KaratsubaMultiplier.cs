using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class KaratsubaMultiplier : IMultiplier
{
    private const int SchoolThreshold = 16;

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        uint[] resultDigits = MultiplyKaratsuba(a.GetDigits(), b.GetDigits());
        bool isNegative = a.IsNegative ^ b.IsNegative;
        return new BetterBigInteger(resultDigits, isNegative);
    }

    private static uint[] MultiplyKaratsuba(ReadOnlySpan<uint> leftDigits, ReadOnlySpan<uint> rightDigits)
    {
        leftDigits = TrimTrailingZeros(leftDigits);
        rightDigits = TrimTrailingZeros(rightDigits);

        if (leftDigits.IsEmpty || rightDigits.IsEmpty)
        {
            return [0];
        }

        int maxLength = Math.Max(leftDigits.Length, rightDigits.Length);
        if (maxLength <= SchoolThreshold)
        {
            return MultiplySchool(leftDigits, rightDigits);
        }

        int split = maxLength / 2;

        ReadOnlySpan<uint> leftLow = leftDigits[..Math.Min(split, leftDigits.Length)];
        ReadOnlySpan<uint> leftHigh = split < leftDigits.Length ? leftDigits[split..] : ReadOnlySpan<uint>.Empty;
        ReadOnlySpan<uint> rightLow = rightDigits[..Math.Min(split, rightDigits.Length)];
        ReadOnlySpan<uint> rightHigh = split < rightDigits.Length ? rightDigits[split..] : ReadOnlySpan<uint>.Empty;

        uint[] z0 = MultiplyKaratsuba(leftLow, rightLow);
        uint[] z2 = MultiplyKaratsuba(leftHigh, rightHigh);

        uint[] leftSum = AddDigits(leftLow, leftHigh);
        uint[] rightSum = AddDigits(rightLow, rightHigh);
        uint[] z1 = MultiplyKaratsuba(leftSum, rightSum);

        uint[] cross = SubtractDigits(SubtractDigits(z1, z2), z0);

        return CombineParts(z0, cross, z2, split);
    }

    private static uint[] CombineParts(ReadOnlySpan<uint> z0, ReadOnlySpan<uint> cross, ReadOnlySpan<uint> z2, int split)
    {
        int resultLength = Math.Max(
            z0.Length,
            Math.Max(cross.Length + split, z2.Length + (split * 2)));

        uint[] result = new uint[resultLength];
        AddShiftedInPlace(result, z0, 0);
        AddShiftedInPlace(result, cross, split);
        AddShiftedInPlace(result, z2, split * 2);
        return Normalize(result);
    }

    private static void AddShiftedInPlace(Span<uint> destination, ReadOnlySpan<uint> source, int shift)
    {
        ulong carry = 0;
        int index = shift;
        int i = 0;

        for (; i < source.Length; i++, index++)
        {
            ulong total = destination[index] + (ulong)source[i] + carry;
            destination[index] = (uint)total;
            carry = total >> 32;
        }

        while (carry > 0)
        {
            ulong total = destination[index] + carry;
            destination[index] = (uint)total;
            carry = total >> 32;
            index++;
        }
    }

    private static uint[] AddDigits(ReadOnlySpan<uint> leftDigits, ReadOnlySpan<uint> rightDigits)
    {
        int maxLength = Math.Max(leftDigits.Length, rightDigits.Length);
        uint[] result = new uint[maxLength + 1];

        ulong carry = 0;
        for (int i = 0; i < maxLength; i++)
        {
            ulong left = i < leftDigits.Length ? leftDigits[i] : 0;
            ulong right = i < rightDigits.Length ? rightDigits[i] : 0;
            ulong total = left + right + carry;
            result[i] = (uint)total;
            carry = total >> 32;
        }

        result[maxLength] = (uint)carry;
        return Normalize(result);
    }

    private static uint[] SubtractDigits(ReadOnlySpan<uint> leftDigits, ReadOnlySpan<uint> rightDigits)
    {
        leftDigits = TrimTrailingZeros(leftDigits);
        rightDigits = TrimTrailingZeros(rightDigits);

        if (CompareMagnitude(leftDigits, rightDigits) < 0)
        {
            throw new ArgumentException("Left operand must be greater than or equal to right operand.");
        }

        uint[] result = new uint[leftDigits.Length];
        long borrow = 0;

        for (int i = 0; i < leftDigits.Length; i++)
        {
            long left = leftDigits[i];
            long right = i < rightDigits.Length ? rightDigits[i] : 0;
            long value = left - right - borrow;

            if (value < 0)
            {
                value += 1L << 32;
                borrow = 1;
            }
            else
            {
                borrow = 0;
            }

            result[i] = (uint)value;
        }

        return Normalize(result);
    }

    private static int CompareMagnitude(ReadOnlySpan<uint> leftDigits, ReadOnlySpan<uint> rightDigits)
    {
        leftDigits = TrimTrailingZeros(leftDigits);
        rightDigits = TrimTrailingZeros(rightDigits);

        if (leftDigits.Length != rightDigits.Length)
        {
            return leftDigits.Length.CompareTo(rightDigits.Length);
        }

        for (int i = leftDigits.Length - 1; i >= 0; i--)
        {
            if (leftDigits[i] == rightDigits[i])
            {
                continue;
            }

            return leftDigits[i] < rightDigits[i] ? -1 : 1;
        }

        return 0;
    }

    private static uint[] MultiplySchool(ReadOnlySpan<uint> leftDigits, ReadOnlySpan<uint> rightDigits)
    {
        if (leftDigits.IsEmpty || rightDigits.IsEmpty)
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

        uint[] result = new uint[accumulator.Length];
        for (int i = 0; i < accumulator.Length; i++)
        {
            result[i] = (uint)accumulator[i];
        }

        return Normalize(result);
    }

    private static ReadOnlySpan<uint> TrimTrailingZeros(ReadOnlySpan<uint> digits)
    {
        int length = digits.Length;
        while (length > 0 && digits[length - 1] == 0)
        {
            length--;
        }

        return digits[..length];
    }

    private static uint[] Normalize(ReadOnlySpan<uint> digits)
    {
        digits = TrimTrailingZeros(digits);
        if (digits.IsEmpty)
        {
            return [0];
        }

        uint[] normalized = new uint[digits.Length];
        digits.CopyTo(normalized);
        return normalized;
    }
}
