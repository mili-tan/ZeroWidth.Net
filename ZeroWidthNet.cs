using System.Text;

public static class ZeroWidthNet
{
    // Zero-width characters (Unicode)
    private const char LeftToRightMark = '\u200E';
    private const char RightToLeftMark = '\u200F';
    private const char ZeroWidthNonJoiner = '\u200C';
    private const char ZeroWidthJoiner = '\u200D';
    private const char ZeroWidthNoBreakSpace = '\uFEFF';
    private const char ZeroWidthSpace = '\u200B';

    // Map: Quinary digit (0-4) → Zero-width character
    private static readonly char[] QuinaryToZeroMap = new[]
    {
        LeftToRightMark,      // 0
        RightToLeftMark,      // 1
        ZeroWidthNonJoiner,   // 2
        ZeroWidthJoiner,      // 3
        ZeroWidthNoBreakSpace // 4
    };

    // Map: Zero-width character → Quinary digit (as string)
    private static readonly Dictionary<char, string> ZeroToQuinaryMap = new()
    {
        { LeftToRightMark, "0" },
        { RightToLeftMark, "1" },
        { ZeroWidthNonJoiner, "2" },
        { ZeroWidthJoiner, "3" },
        { ZeroWidthNoBreakSpace, "4" },
        { ZeroWidthSpace, "5" } // Used only as separator, not for encoding
    };

    /// <summary>
    /// Converts plain text to a zero-width encoded string
    /// </summary>
    public static string TextToZeroWidth(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var zeroParts = new List<string>();
        for (int i = 0; i < text.Length; i++)
        {
            int codePoint = char.ConvertToUtf32(text, i);
            string base5 = ConvertToZBase5(codePoint);
            var zeroPart = new StringBuilder();
            foreach (char c in base5)
            {
                int digit = c - '0';
                zeroPart.Append(QuinaryToZeroMap[digit]);
            }
            zeroParts.Add(zeroPart.ToString());
            if (codePoint > 0xFFFF) i++; // Skip low surrogate in surrogate pairs
        }
        return string.Join(ZeroWidthSpace, zeroParts);
    }

    ///// <summary>
    ///// Decodes a zero-width encoded string back to plain text
    ///// </summary>
    //public static string ZeroWidthToText(string zeroWidth)
    //{
    //    if (string.IsNullOrEmpty(zeroWidth)) return string.Empty;

    //    Console.WriteLine("ZWS:"+(Convert.ToInt64(ZeroWidthSpace)));

    //    var parts = new List<string>();
    //    var currentPart = new StringBuilder();

    //    foreach (char c in zeroWidth)
    //    {
    //        Console.WriteLine(Convert.ToInt64(c));
    //        Console.WriteLine(Convert.ToInt64(c) == Convert.ToInt64(ZeroWidthSpace));
    //        if (Convert.ToInt64(c) == Convert.ToInt64(ZeroWidthSpace))
    //        {
    //            if (currentPart.Length > 0)
    //            {
    //                parts.Add(currentPart.ToString());
    //                currentPart.Clear();
    //            }
    //        }
    //        else
    //        {
    //            currentPart.Append(c);
    //        }
    //    }

    //    if (currentPart.Length > 0)
    //    {
    //        parts.Add(currentPart.ToString());
    //    }

    //    Console.WriteLine(parts.Count);
    //    var text = new StringBuilder();
    //    foreach (string part in parts)
    //    {
    //        string base5 = "";
    //        foreach (char c in part)
    //        {
    //            if (ZeroToQuinaryMap.TryGetValue(c, out string digit))
    //                base5 += digit;
    //            else
    //                throw new ArgumentException($"Invalid zero-width character: {(int)c:X4}");
    //        }
    //        text.Append(Base5Decode(base5));
    //    }
    //    return text.ToString();
    //}

    /// <summary>
    /// Embeds hidden text into visible text at a specified position
    /// </summary>
    /// <param name="visibleText">Visible text to display</param>
    /// <param name="hiddenText">Hidden text to encode</param>
    /// <param name="insertPosition">
    /// Position to insert hidden content (0-based index).
    /// If position exceeds visible text length, hidden content is appended.
    /// </param>
    public static string Encode(string visibleText, string hiddenText, int insertPosition)
    {
        string hiddenZeroWidth = TextToZeroWidth(hiddenText);
        if (string.IsNullOrEmpty(visibleText)) return hiddenZeroWidth;

        // Normalize position
        insertPosition = Math.Clamp(insertPosition, 0, visibleText.Length);

        var encoded = new StringBuilder(visibleText.Length + hiddenZeroWidth.Length);
        if (insertPosition == 0)
        {
            encoded.Append(hiddenZeroWidth);
            encoded.Append(visibleText);
        }
        else
        {
            // Insert at specified position
            for (int i = 0; i < visibleText.Length; i++)
            {
                encoded.Append(visibleText[i]);
                if (i == insertPosition - 1) // Insert after the specified index
                {
                    encoded.Append(hiddenZeroWidth);
                }
            }
            // Handle append to end
            if (insertPosition >= visibleText.Length)
            {
                encoded.Append(hiddenZeroWidth);
            }
        }
        return encoded.ToString();
    }

    /// <summary>
    /// Original Encode method (insert after first character for backward compatibility)
    /// </summary>
    public static string Encode(string visibleText, string hiddenText)
    {
        try
        {
            return Encode(visibleText, hiddenText,
                insertPosition: visibleText.IndexOfAny([' ']));
        }
        catch (Exception)
        {
            return Encode(visibleText, hiddenText, insertPosition: 1); // Default to after first char
        }
    }

    /// <summary>
    /// Extracts visible and hidden parts from encoded text
    /// </summary>
    public static (string Visible, string Hidden) Extract(string text)
    {
        var visible = new StringBuilder();
        var hidden = new StringBuilder();
        foreach (char c in text)
        {
            if (ZeroToQuinaryMap.ContainsKey(c) && c != ZeroWidthSpace)
                hidden.Append(c);
            else
                visible.Append(c);
        }
        return (visible.ToString(), hidden.ToString());
    }

    ///// <summary>
    ///// Decodes hidden content from visible text
    ///// </summary>
    //public static string Decode(string encodedText)
    //{
    //    var (_, hidden) = Extract(encodedText);
    //    return ZeroWidthToText(hidden);
    //}

    /// <summary>
    /// Converts a decimal number to base-5 representation
    /// </summary>
    private static string ConvertToZBase5(int number)
    {
        if (number == 0) return "0";
        var base5 = new StringBuilder();
        while (number > 0)
        {
            base5.Insert(0, number % 5);
            number /= 5;
        }
        return base5.ToString();
    }

    ///// <summary>
    ///// Decode base-5 string to decimal integer
    ///// </summary>
    //public static char ZBase5Decode(string base5String)
    //{
    //    foreach (char c in base5String)
    //    {
    //        if (c < '0' || c > '4')
    //            throw new ArgumentException("Invalid Base5 digit");
    //    }

    //    BigInteger decimalValue = 0;
    //    for (int i = 0; i < base5String.Length; i++)
    //    {
    //        decimalValue = decimalValue * 5 + (base5String[i] - '0');
    //    }

    //    if (decimalValue > char.MaxValue)
    //        throw new OverflowException("Decoded value exceeds Char.MAX_VALUE");

    //    return (char)decimalValue;
    //}

}