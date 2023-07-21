namespace GameFramework
{
    public static partial class Utility
    {
        /// <summary>
        /// 这是一个有破坏性且危险的优化，但可以有效优化各种 buffer 所产生的 GCAlloc。
        /// 使用此 GlobalBuffer 的原则是：
        /// 1. 单线程下
        /// 2. 注意使用 GlobalBuffer 的时机，因为大家都在共用它
        /// </summary>
        public static class GlobalBuffer
        {
            private const int BlockSize = 1024 * 4;
            private static byte[] s_CachedBytes = null;

            static GlobalBuffer()
            {
                EnsureCachedBytesSize(1024 * 1024 * 16);
            }

            /// <summary>
            /// 获取缓冲二进制流。
            /// </summary>
            /// <returns></returns>
            public static byte[] CachedBytes
            {
                get
                {
                    return s_CachedBytes;
                }
            }

            /// <summary>
            /// 获取缓冲二进制流的大小。
            /// </summary>
            public static int CachedBytesSize
            {
                get
                {
                    return s_CachedBytes != null ? s_CachedBytes.Length : 0;
                }
            }

            /// <summary>
            /// 确保二进制流缓存分配足够大小的内存并缓存。
            /// </summary>
            /// <param name="ensureSize">要确保二进制流缓存分配内存的大小。</param>
            public static void EnsureCachedBytesSize(int ensureSize)
            {
                if (ensureSize < 0)
                {
                    throw new GameFrameworkException("Ensure size is invalid.");
                }

                if (s_CachedBytes == null || s_CachedBytes.Length < ensureSize)
                {
                    FreeCachedBytes();
                    int size = (ensureSize - 1 + BlockSize) / BlockSize * BlockSize;
                    s_CachedBytes = new byte[size];
                }
            }

            /// <summary>
            /// 释放缓存的二进制流。
            /// </summary>
            public static void FreeCachedBytes()
            {
                s_CachedBytes = null;
            }
        }
    }
}
