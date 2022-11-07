namespace Momento.Etl.Model;

public static class Extensions
{
    public static long ToUnixTimeMilliseconds(this DateTime dateTime)
    {
        return (new DateTimeOffset(dateTime)).ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// On the wire size of a UTF-8 encodied string.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static int PayloadSizeInBytes(this string s)
    {
        if (s is null)
        {
            return 0;
        }
        return System.Text.Encoding.UTF8.GetByteCount(s);
    }
}
