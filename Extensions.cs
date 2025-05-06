using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Cruncher
{
    public static class Extensions
    {
        public static bool IsIdentifier(this char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '/';
        }

        public static string NormaliseDirectory(this string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            //Remove any trailing slashes
            while (path.EndsWith('/') || path.EndsWith('\\'))
                path = path[0..^1];

            //To keep consistency between platforms, we will always prefer forward slashes
            //since Windows is the only platform that uses backslashes BUT also supports forward slashes
            path = path.Replace('\\', '/');

            return path;
        }

        //https://www.azillionmonkeys.com/qed/hash.html
        public static ulong Hash(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;

            byte[] data = Encoding.UTF8.GetBytes(str);
            int len = data.Length;

            ulong hash = (ulong)len;
            ulong tmp;
            int rem = len & 3;
            len >>= 2;

            int offset = 0;

            while (len-- > 0)
            {
                hash += Get16Bits(data, offset);
                tmp = ((ulong)Get16Bits(data, offset + 2) << 11) ^ hash;
                hash = (hash << 16) ^ tmp;
                offset += 4;
                hash += hash >> 11;
            }

            switch (rem)
            {
                case 3:
                    hash += Get16Bits(data, offset);
                    hash ^= hash << 16;
                    hash ^= ((ulong)data[offset + 2] << 18);
                    hash += hash >> 11;
                    break;
                case 2:
                    hash += Get16Bits(data, offset);
                    hash ^= hash << 11;
                    hash += hash >> 17;
                    break;
                case 1:
                    hash += data[offset];
                    hash ^= hash << 10;
                    hash += hash >> 1;
                    break;
            }

            hash ^= hash << 3;
            hash += hash >> 5;
            hash ^= hash << 4;
            hash += hash >> 17;
            hash ^= hash << 25;
            hash += hash >> 6;

            return hash;
        }

        private static ushort Get16Bits(byte[] data, int index)
        {
            return (ushort)(data[index] | (data[index + 1] << 8));
        }
    }
}