using System.Numerics;
using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class FftMultiplier : IMultiplier
{
    private const ulong FftBase = 1UL << 16;
    private const uint FftMask = (1U << 16) - 1;

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        uint[] resultDigits = MultiplyFft(a.GetDigits(), b.GetDigits());
        bool isNegative = a.IsNegative ^ b.IsNegative;
        return new BetterBigInteger(resultDigits, isNegative);
    }

    private static uint[] MultiplyFft(ReadOnlySpan<uint> leftDigits, ReadOnlySpan<uint> rightDigits)
    {
        leftDigits = TrimTrailingZeros(leftDigits);
        rightDigits = TrimTrailingZeros(rightDigits);

        if (leftDigits.IsEmpty || rightDigits.IsEmpty)
        {
            return [0];
        }

        ulong[] leftBase16 = ExpandToBase16(leftDigits);
        ulong[] rightBase16 = ExpandToBase16(rightDigits);

        int convolutionLength = leftBase16.Length + rightBase16.Length;
        int fftLength = 1;
        while (fftLength < convolutionLength)
        {
            fftLength <<= 1;
        }

        Complex[] left = new Complex[fftLength];
        Complex[] right = new Complex[fftLength];

        for (int i = 0; i < leftBase16.Length; i++)
        {
            left[i] = new Complex(leftBase16[i], 0);
        }

        for (int i = 0; i < rightBase16.Length; i++)
        {
            right[i] = new Complex(rightBase16[i], 0);
        }

        Transform(left, invert: false);
        Transform(right, invert: false);

        for (int i = 0; i < fftLength; i++)
        {
            left[i] *= right[i];
        }

        Transform(left, invert: true);

        ulong[] convolution = new ulong[convolutionLength];
        for (int i = 0; i < convolutionLength; i++)
        {
            convolution[i] = (ulong)Math.Round(left[i].Real);
        }

        ulong carry = 0;
        for (int i = 0; i < convolution.Length; i++)
        {
            ulong total = convolution[i] + carry;
            convolution[i] = total % FftBase;
            carry = total / FftBase;
        }

        if (carry > 0)
        {
            Array.Resize(ref convolution, convolution.Length + 1);
            convolution[^1] = carry;
        }

        return PackBase16ToUInt32(convolution);
    }

    private static ulong[] ExpandToBase16(ReadOnlySpan<uint> digits)
    {
        ulong[] expanded = new ulong[digits.Length * 2];
        for (int i = 0; i < digits.Length; i++)
        {
            expanded[i * 2] = digits[i] & FftMask;
            expanded[(i * 2) + 1] = digits[i] >> 16;
        }

        return TrimTrailingZeros(expanded);
    }

    private static uint[] PackBase16ToUInt32(ReadOnlySpan<ulong> digits)
    {
        digits = TrimTrailingZeros(digits);
        if (digits.IsEmpty)
        {
            return [0];
        }

        int wordCount = (digits.Length + 1) / 2;
        uint[] packed = new uint[wordCount];

        for (int i = 0; i < wordCount; i++)
        {
            ulong low = digits[i * 2];
            ulong high = (i * 2) + 1 < digits.Length ? digits[(i * 2) + 1] : 0;
            packed[i] = (uint)(low | (high << 16));
        }

        return Normalize(packed);
    }

    private static void Transform(Complex[] values, bool invert)
    {
        int n = values.Length;

        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            while ((j & bit) != 0)
            {
                j ^= bit;
                bit >>= 1;
            }

            j ^= bit;
            if (i < j)
            {
                (values[i], values[j]) = (values[j], values[i]);
            }
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            double angle = (2 * Math.PI / len) * (invert ? -1 : 1);
            Complex root = new(Math.Cos(angle), Math.Sin(angle));

            for (int i = 0; i < n; i += len)
            {
                Complex factor = Complex.One;
                int half = len >> 1;

                for (int j = 0; j < half; j++)
                {
                    Complex even = values[i + j];
                    Complex odd = values[i + j + half] * factor;
                    values[i + j] = even + odd;
                    values[i + j + half] = even - odd;
                    factor *= root;
                }
            }
        }

        if (!invert)
        {
            return;
        }

        for (int i = 0; i < n; i++)
        {
            values[i] /= n;
        }
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

    private static ulong[] TrimTrailingZeros(ulong[] digits)
    {
        int length = digits.Length;
        while (length > 0 && digits[length - 1] == 0)
        {
            length--;
        }

        if (length == digits.Length)
        {
            return digits;
        }

        ulong[] trimmed = new ulong[length];
        Array.Copy(digits, trimmed, length);
        return trimmed;
    }

    private static ReadOnlySpan<ulong> TrimTrailingZeros(ReadOnlySpan<ulong> digits)
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
