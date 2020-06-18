using System;
using System.Linq;
using System.Text;

namespace HBaseNet.Utility
{
    public static class BinaryEx
    {
        public static byte[] Initialize(this byte[] arr, byte value)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }

            return arr;
        }

        public static byte[] ConcatInOrder(params byte[][] item)
        {
            var len = item.Sum(t => t.Length);
            var result = new byte[len];
            var pos = 0;
            foreach (var arr in item)
            {
                arr.CopyTo(result, pos);
                pos += arr.Length;
            }

            return result;
        }

        public static int Compare(byte[] bytes1, byte[] bytes2)
        {
            if (bytes1.Length != bytes2.Length) return (bytes1.Length - bytes2.Length);
            return Compare(bytes1, 0, bytes2, 0, Math.Min(bytes1.Length, bytes2.Length));
        }

        public static int Compare(byte[] bytes1, int offset1, byte[] bytes2, int offset2, int count)
        {
            var span1 = bytes1.AsSpan(offset1, count);
            var span2 = bytes2.AsSpan(offset2, count);

            return span1.SequenceCompareTo(span2);
        }

        public static byte[] ToUtf8Bytes(this string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public static string ToUtf8String(this byte[] arr)
        {
            return Encoding.UTF8.GetString(arr);
        }

        public static int GetHash(this byte[] arr)
        {
            return arr?.Any() != true ? 0 : arr.Aggregate(17, (current, @by) => current << 31 + @by);
        }
    }
}