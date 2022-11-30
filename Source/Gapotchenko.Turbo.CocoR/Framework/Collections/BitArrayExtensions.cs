using System.Collections;

namespace Gapotchenko.Turbo.CocoR.Framework.Collections;

static class BitArrayExtensions
{
    public static int GetCountOfSetBits(this BitArray s)
    {
        int count = 0;

        int n = s.Count;
        for (int i = 0; i < n; ++i)
        {
            if (s[i])
                ++count;
        }

        return count;
    }

    public static bool SetEquals(this BitArray a, BitArray b)
    {
        int n = a.Count;
        if (b.Count != n)
            return false;

        for (int i = 0; i < n; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    public static bool Overlaps(this BitArray a, BitArray b)
    {
        // a * b != {}
        int n = a.Count;
        for (int i = 0; i < n; i++)
        {
            if (a[i] && b[i])
                return true;
        }
        return false;
    }

    public static void ExceptWith(this BitArray a, BitArray b)
    {
        // a = a - b
        var _b = (BitArray)b.Clone();
        a.And(_b.Not());
    }
}
