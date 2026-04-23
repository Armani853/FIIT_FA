using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private const string DigitsAlphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int SimpleMultiplierThreshold = 16;
    private const int KaratsubaMultiplierThreshold = 64;

    private int _signBit;
    private uint _smallValue;
    private uint[]? _data;

    public bool IsNegative => _signBit == 1;

    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        ArgumentNullException.ThrowIfNull(digits);
        InitializeFromDigits(digits, isNegative);
    }

    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
    {
        ArgumentNullException.ThrowIfNull(digits);
        InitializeFromDigits(digits as uint[] ?? [.. digits], isNegative);
    }

    public BetterBigInteger(string value, int radix)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);

        if (radix is < 2 or > 36)
        {
            throw new ArgumentOutOfRangeException(nameof(radix), "Radix must be in range [2, 36].");
        }

        string trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new FormatException("Input string cannot be empty.");
        }

        bool isNegative = false;
        int startIndex = 0;

        if (trimmed[0] is '+' or '-')
        {
            isNegative = trimmed[0] == '-';
            startIndex = 1;
        }

        if (startIndex >= trimmed.Length)
        {
            throw new FormatException("Input string does not contain digits.");
        }

        uint[] magnitude = [0];
        for (int i = startIndex; i < trimmed.Length; i++)
        {
            int digit = ParseDigit(trimmed[i]);
            if (digit >= radix)
            {
                throw new FormatException($"Digit '{trimmed[i]}' is not valid for radix {radix}.");
            }

            magnitude = MultiplyMagnitudeByUInt(magnitude, (uint)radix);
            magnitude = AddMagnitudeUInt(magnitude, (uint)digit);
        }

        InitializeFromDigits(magnitude, isNegative);
    }

    private BetterBigInteger()
    {
    }

    public ReadOnlySpan<uint> GetDigits() => _data ?? [_smallValue];

    public int CompareTo(IBigInteger? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        if (other is not BetterBigInteger otherBigInt)
        {
            throw new ArgumentException("Compared value must be BetterBigInteger.", nameof(other));
        }

        if (IsNegative != otherBigInt.IsNegative)
        {
            return IsNegative ? -1 : 1;
        }

        int magnitudeComparison = CompareMagnitude(this, otherBigInt);
        return IsNegative ? -magnitudeComparison : magnitudeComparison;
    }

    public bool Equals(IBigInteger? other)
    {
        if (other is not BetterBigInteger otherBigInt)
        {
            return false;
        }

        return _signBit == otherBigInt._signBit && GetDigits().SequenceEqual(otherBigInt.GetDigits());
    }

    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(_signBit);

        foreach (uint digit in GetDigits())
        {
            hash.Add(digit);
        }

        return hash.ToHashCode();
    }

    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.IsNegative == b.IsNegative)
        {
            return new BetterBigInteger(AddMagnitudes(a.GetDigits(), b.GetDigits()), a.IsNegative);
        }

        int comparison = CompareMagnitude(a, b);
        if (comparison == 0)
        {
            return Zero();
        }

        return comparison > 0
            ? new BetterBigInteger(SubtractMagnitudes(a.GetDigits(), b.GetDigits()), a.IsNegative)
            : new BetterBigInteger(SubtractMagnitudes(b.GetDigits(), a.GetDigits()), b.IsNegative);
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return a + (-b);
    }

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (a.IsZero)
        {
            return Zero();
        }

        return new BetterBigInteger(a.GetDigits().ToArray(), !a.IsNegative);
    }

    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (b.IsZero)
        {
            throw new DivideByZeroException();
        }

        DivideAndRemainder(a, b, out BetterBigInteger quotient, out _);
        return quotient;
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (b.IsZero)
        {
            throw new DivideByZeroException();
        }

        DivideAndRemainder(a, b, out _, out BetterBigInteger remainder);
        return remainder;
    }

    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        IMultiplier multiplier = SelectMultiplier(a, b);
        return multiplier.Multiply(a, b);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        ArgumentNullException.ThrowIfNull(a);
        return ApplyBitwiseUnary(a, static value => ~value);
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return ApplyBitwiseBinary(a, b, static (left, right) => left & right);
    }

    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return ApplyBitwiseBinary(a, b, static (left, right) => left | right);
    }

    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return ApplyBitwiseBinary(a, b, static (left, right) => left ^ right);
    }

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (shift < 0)
        {
            return a >> -shift;
        }

        if (shift == 0 || a.IsZero)
        {
            return new BetterBigInteger(a.GetDigits().ToArray(), a.IsNegative);
        }

        uint[] shifted = ShiftLeftMagnitude(a.GetDigits(), shift);
        return new BetterBigInteger(shifted, a.IsNegative);
    }

    public static BetterBigInteger operator >>(BetterBigInteger a, int shift)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (shift < 0)
        {
            return a << -shift;
        }

        if (shift == 0 || a.IsZero)
        {
            return new BetterBigInteger(a.GetDigits().ToArray(), a.IsNegative);
        }

        uint[] quotientMagnitude = ShiftRightMagnitude(a.GetDigits(), shift, out bool hadRemainder);
        if (!a.IsNegative)
        {
            return new BetterBigInteger(quotientMagnitude, false);
        }

        if (!hadRemainder)
        {
            return new BetterBigInteger(quotientMagnitude, true);
        }

        return new BetterBigInteger(AddMagnitudeUInt(quotientMagnitude, 1), true);
    }

    public static bool operator ==(BetterBigInteger? a, BetterBigInteger? b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger? a, BetterBigInteger? b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;

    public override string ToString() => ToString(10);

    public string ToString(int radix)
    {
        if (radix is < 2 or > 36)
        {
            throw new ArgumentOutOfRangeException(nameof(radix), "Radix must be in range [2, 36].");
        }

        if (IsZero)
        {
            return "0";
        }

        uint[] remaining = GetDigits().ToArray();
        List<char> chars = [];

        while (!IsZeroMagnitude(remaining))
        {
            remaining = DivideMagnitudeByUInt(remaining, (uint)radix, out uint remainder);
            chars.Add(DigitsAlphabet[(int)remainder]);
        }

        if (IsNegative)
        {
            chars.Add('-');
        }

        chars.Reverse();
        return new string([.. chars]);
    }

    internal static BetterBigInteger MultiplyCore(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        return SelectMultiplier(a, b).Multiply(a, b);
    }

    private bool IsZero => _data is null ? _smallValue == 0 : _data.Length == 1 && _data[0] == 0;

    private void InitializeFromDigits(uint[] digits, bool isNegative)
    {
        uint[] normalized = NormalizeDigits(digits);

        if (normalized.Length == 1)
        {
            _smallValue = normalized[0];
            _data = null;
        }
        else
        {
            _smallValue = 0;
            _data = normalized;
        }

        _signBit = isNegative && !IsZeroMagnitude(normalized) ? 1 : 0;
    }

    private static BetterBigInteger Zero() => new([0], false);

    private static BetterBigInteger ApplyBitwiseUnary(BetterBigInteger value, Func<uint, uint> operation)
    {
        int wordCount = value.GetDigits().Length + 1;
        uint[] words = ToTwosComplement(value, wordCount);

        for (int i = 0; i < words.Length; i++)
        {
            words[i] = operation(words[i]);
        }

        return FromTwosComplement(words);
    }

    private static BetterBigInteger ApplyBitwiseBinary(BetterBigInteger left, BetterBigInteger right, Func<uint, uint, uint> operation)
    {
        int wordCount = Math.Max(left.GetDigits().Length, right.GetDigits().Length) + 1;
        uint[] leftWords = ToTwosComplement(left, wordCount);
        uint[] rightWords = ToTwosComplement(right, wordCount);
        uint[] result = new uint[wordCount];

        for (int i = 0; i < wordCount; i++)
        {
            result[i] = operation(leftWords[i], rightWords[i]);
        }

        return FromTwosComplement(result);
    }

    private static uint[] ToTwosComplement(BetterBigInteger value, int wordCount)
    {
        uint[] result = new uint[wordCount];
        ReadOnlySpan<uint> digits = value.GetDigits();

        for (int i = 0; i < digits.Length && i < wordCount; i++)
        {
            result[i] = digits[i];
        }

        if (!value.IsNegative)
        {
            return result;
        }

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = ~result[i];
        }

        ulong carry = 1;
        for (int i = 0; i < result.Length && carry > 0; i++)
        {
            ulong total = result[i] + carry;
            result[i] = (uint)total;
            carry = total >> 32;
        }

        return result;
    }

    private static BetterBigInteger FromTwosComplement(uint[] words)
    {
        if (words.Length == 0)
        {
            return Zero();
        }

        bool isNegative = (words[^1] & 0x80000000u) != 0;
        uint[] canonicalWords = CanonicalizeTwosComplement(words, isNegative);

        if (!isNegative)
        {
            return new BetterBigInteger(NormalizeDigits(canonicalWords), false);
        }

        uint[] magnitude = new uint[canonicalWords.Length];
        Array.Copy(canonicalWords, magnitude, canonicalWords.Length);

        ulong borrow = 1;
        for (int i = 0; i < magnitude.Length && borrow > 0; i++)
        {
            if (magnitude[i] >= borrow)
            {
                magnitude[i] -= (uint)borrow;
                borrow = 0;
            }
            else
            {
                magnitude[i] = uint.MaxValue;
                borrow = 1;
            }
        }

        for (int i = 0; i < magnitude.Length; i++)
        {
            magnitude[i] = ~magnitude[i];
        }
        return new BetterBigInteger(NormalizeDigits(magnitude), true);
    }

    private static uint[] CanonicalizeTwosComplement(ReadOnlySpan<uint> words, bool isNegative)
    {
        int length = words.Length;
        if (isNegative)
        {
            while (length > 1 && words[length - 1] == uint.MaxValue && (words[length - 2] & 0x80000000u) != 0)
            {
                length--;
            }
        }
        else
        {
            while (length > 1 && words[length - 1] == 0 && (words[length - 2] & 0x80000000u) == 0)
            {
                length--;
            }
        }

        uint[] result = new uint[length];
        words[..length].CopyTo(result);
        return result;
    }

    private static void DivideAndRemainder(BetterBigInteger dividend, BetterBigInteger divisor, out BetterBigInteger quotient, out BetterBigInteger remainder)
    {
        int comparison = CompareMagnitude(dividend, divisor);
        if (comparison < 0)
        {
            quotient = Zero();
            remainder = new BetterBigInteger(dividend.GetDigits().ToArray(), dividend.IsNegative);
            return;
        }

        if (comparison == 0)
        {
            quotient = new BetterBigInteger([1], dividend.IsNegative ^ divisor.IsNegative);
            remainder = Zero();
            return;
        }

        uint[] quotientMagnitude = DivideMagnitude(dividend.GetDigits(), divisor.GetDigits(), out uint[] remainderMagnitude);
        quotient = new BetterBigInteger(quotientMagnitude, dividend.IsNegative ^ divisor.IsNegative);
        remainder = new BetterBigInteger(remainderMagnitude, dividend.IsNegative);
    }

    private static uint[] DivideMagnitude(ReadOnlySpan<uint> dividend, ReadOnlySpan<uint> divisor, out uint[] remainder)
    {
        dividend = TrimTrailingZeros(dividend);
        divisor = TrimTrailingZeros(divisor);

        if (divisor.IsEmpty)
        {
            throw new DivideByZeroException();
        }

        if (CompareMagnitude(dividend, divisor) < 0)
        {
            remainder = NormalizeDigits(dividend);
            return [0];
        }

        if (divisor.Length == 1)
        {
            uint[] quotientSmall = DivideMagnitudeByUInt(dividend.ToArray(), divisor[0], out uint rem);
            remainder = [rem];
            return quotientSmall;
        }

        int bitLength = GetBitLength(dividend);
        uint[] quotient = new uint[(bitLength + 31) / 32];
        uint[] currentRemainder = [0];

        for (int bitIndex = bitLength - 1; bitIndex >= 0; bitIndex--)
        {
            currentRemainder = ShiftLeftOneAndAddBit(currentRemainder, GetBit(dividend, bitIndex));
            if (CompareMagnitude(currentRemainder, divisor) >= 0)
            {
                currentRemainder = SubtractMagnitudes(currentRemainder, divisor);
                SetBit(quotient, bitIndex);
            }
        }

        remainder = NormalizeDigits(currentRemainder);
        return NormalizeDigits(quotient);
    }

    private static uint[] ShiftLeftOneAndAddBit(ReadOnlySpan<uint> digits, bool bit)
    {
        uint[] result = new uint[digits.Length + 1];
        ulong carry = bit ? 1UL : 0UL;

        for (int i = 0; i < digits.Length; i++)
        {
            ulong value = ((ulong)digits[i] << 1) | carry;
            result[i] = (uint)value;
            carry = value >> 32;
        }

        result[^1] = (uint)carry;
        return NormalizeDigits(result);
    }

    private static int GetBitLength(ReadOnlySpan<uint> digits)
    {
        digits = TrimTrailingZeros(digits);
        if (digits.IsEmpty)
        {
            return 0;
        }

        uint highest = digits[^1];
        int highBits = 32;
        while ((highest & 0x80000000u) == 0)
        {
            highest <<= 1;
            highBits--;
        }

        return ((digits.Length - 1) * 32) + highBits;
    }

    private static bool GetBit(ReadOnlySpan<uint> digits, int bitIndex)
    {
        int digitIndex = bitIndex / 32;
        int bitOffset = bitIndex % 32;
        return digitIndex < digits.Length && ((digits[digitIndex] >> bitOffset) & 1u) != 0;
    }

    private static void SetBit(Span<uint> digits, int bitIndex)
    {
        int digitIndex = bitIndex / 32;
        int bitOffset = bitIndex % 32;
        digits[digitIndex] |= 1u << bitOffset;
    }

    private static uint[] ShiftLeftMagnitude(ReadOnlySpan<uint> digits, int shift)
    {
        digits = TrimTrailingZeros(digits);
        if (digits.IsEmpty)
        {
            return [0];
        }

        int wordShift = shift / 32;
        int bitShift = shift % 32;
        uint[] result = new uint[digits.Length + wordShift + 1];
        ulong carry = 0;

        for (int i = 0; i < digits.Length; i++)
        {
            ulong value = ((ulong)digits[i] << bitShift) | carry;
            result[i + wordShift] = (uint)value;
            carry = value >> 32;
        }

        result[digits.Length + wordShift] = (uint)carry;
        return NormalizeDigits(result);
    }

    private static uint[] ShiftRightMagnitude(ReadOnlySpan<uint> digits, int shift, out bool hadRemainder)
    {
        digits = TrimTrailingZeros(digits);
        if (digits.IsEmpty)
        {
            hadRemainder = false;
            return [0];
        }

        int wordShift = shift / 32;
        int bitShift = shift % 32;
        hadRemainder = false;

        if (wordShift >= digits.Length)
        {
            hadRemainder = !IsZeroMagnitude(digits);
            return [0];
        }

        for (int i = 0; i < wordShift; i++)
        {
            if (digits[i] != 0)
            {
                hadRemainder = true;
                break;
            }
        }

        int resultLength = digits.Length - wordShift;
        uint[] result = new uint[resultLength];
        ulong carry = 0;

        for (int sourceIndex = digits.Length - 1; sourceIndex >= wordShift; sourceIndex--)
        {
            ulong current = digits[sourceIndex];
            int resultIndex = sourceIndex - wordShift;

            if (bitShift == 0)
            {
                result[resultIndex] = (uint)current;
            }
            else
            {
                result[resultIndex] = (uint)((current >> bitShift) | carry);
                carry = (current << (32 - bitShift)) & 0xFFFFFFFFUL;
            }
        }

        if (bitShift != 0 && carry != 0)
        {
            hadRemainder = true;
        }

        return NormalizeDigits(result);
    }

    private static uint[] AddMagnitudes(ReadOnlySpan<uint> leftDigits, ReadOnlySpan<uint> rightDigits)
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
        return NormalizeDigits(result);
    }

    private static uint[] SubtractMagnitudes(ReadOnlySpan<uint> leftDigits, ReadOnlySpan<uint> rightDigits)
    {
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
            long difference = left - right - borrow;

            if (difference < 0)
            {
                difference += 1L << 32;
                borrow = 1;
            }
            else
            {
                borrow = 0;
            }

            result[i] = (uint)difference;
        }

        return NormalizeDigits(result);
    }

    private static uint[] MultiplyMagnitudeByUInt(ReadOnlySpan<uint> digits, uint multiplier)
    {
        digits = TrimTrailingZeros(digits);
        if (digits.IsEmpty || multiplier == 0)
        {
            return [0];
        }

        if (multiplier == 1)
        {
            return NormalizeDigits(digits);
        }

        uint[] result = new uint[digits.Length + 1];
        ulong carry = 0;

        for (int i = 0; i < digits.Length; i++)
        {
            ulong product = ((ulong)digits[i] * multiplier) + carry;
            result[i] = (uint)product;
            carry = product >> 32;
        }

        result[^1] = (uint)carry;
        return NormalizeDigits(result);
    }

    private static uint[] AddMagnitudeUInt(ReadOnlySpan<uint> digits, uint value)
    {
        if (value == 0)
        {
            return NormalizeDigits(digits);
        }

        uint[] result = new uint[Math.Max(1, digits.Length) + 1];
        if (!digits.IsEmpty)
        {
            digits.CopyTo(result);
        }

        ulong carry = value;
        for (int i = 0; i < result.Length && carry > 0; i++)
        {
            ulong total = result[i] + carry;
            result[i] = (uint)total;
            carry = total >> 32;
        }

        return NormalizeDigits(result);
    }

    private static uint[] DivideMagnitudeByUInt(ReadOnlySpan<uint> digits, uint divisor, out uint remainder)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException();
        }

        digits = TrimTrailingZeros(digits);
        if (digits.IsEmpty)
        {
            remainder = 0;
            return [0];
        }

        uint[] quotient = new uint[digits.Length];
        ulong rem = 0;

        for (int i = digits.Length - 1; i >= 0; i--)
        {
            ulong current = (rem << 32) | digits[i];
            quotient[i] = (uint)(current / divisor);
            rem = current % divisor;
        }

        remainder = (uint)rem;
        return NormalizeDigits(quotient);
    }

    private static IMultiplier SelectMultiplier(BetterBigInteger a, BetterBigInteger b)
    {
        int maxDigits = Math.Max(a.GetDigits().Length, b.GetDigits().Length);

        if (maxDigits < SimpleMultiplierThreshold)
        {
            return new SimpleMultiplier();
        }

        if (maxDigits < KaratsubaMultiplierThreshold)
        {
            return new KaratsubaMultiplier();
        }

        return new FftMultiplier();
    }

    private static uint[] NormalizeDigits(ReadOnlySpan<uint> digits)
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

    private static ReadOnlySpan<uint> TrimTrailingZeros(ReadOnlySpan<uint> digits)
    {
        int length = digits.Length;
        while (length > 0 && digits[length - 1] == 0)
        {
            length--;
        }

        return digits[..length];
    }

    private static bool IsZeroMagnitude(ReadOnlySpan<uint> digits)
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

    private static int CompareMagnitude(BetterBigInteger left, BetterBigInteger right) => CompareMagnitude(left.GetDigits(), right.GetDigits());

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

    private static int ParseDigit(char c)
    {
        char normalized = char.ToUpperInvariant(c);
        int value = DigitsAlphabet.IndexOf(normalized, StringComparison.Ordinal);
        return value >= 0 ? value : throw new FormatException($"Invalid digit '{c}'.");
    }
}
