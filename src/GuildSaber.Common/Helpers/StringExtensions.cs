namespace GuildSaber.Common.Helpers;

public static class StringExtensions
{
    /// <param name="value">String to truncate</param>
    extension(string value)
    {
        /// <summary>
        /// Truncate the string considering words, not characters.
        /// </summary>
        /// <param name="length">Max length of the resulting string</param>
        /// <returns>Truncated string to the last word prior the defined length</returns>
        /// <remarks>This function is old and I won't spend the time to optimize it.</remarks>
        public string TruncateWithWords(int length)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            /* worse case with " ..." */
            length -= 3;

            var returnValue = value;
            if (value.Length <= length) return returnValue;

            var tmp = value[..length];
            if (tmp.LastIndexOf(' ') > 0)
                returnValue = tmp[..tmp.LastIndexOf(' ')] + " …";
            return returnValue;
        }

        /// <summary>
        /// Truncate the string considering characters, not words.
        /// </summary>
        /// <param name="length">Max length of the resulting string</param>
        /// <returns>Truncated string to the defined length</returns>
        public string Truncate(int length)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= length)
                return value;

#if NET10_0_OR_GREATER
            return string.Concat(value.AsSpan(0, length - 1), "…");
#else
            return value[..(length - 2)] + "…";
#endif
        }
    }
}