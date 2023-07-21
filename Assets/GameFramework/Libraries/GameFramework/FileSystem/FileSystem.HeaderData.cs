//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace GameFramework.FileSystem
{
    internal sealed partial class FileSystem : IFileSystem
    {
        /// <summary>
        /// 头数据。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct HeaderData
        {
            private const int HeaderLength = 3;
            private const int FileSystemVersion = 100;
            private const int EncryptBytesLength = 4;
            private static readonly byte[] Header = new byte[HeaderLength] { (byte)'G', (byte)'F', (byte)'F' };

            private const int VerifyKeyLength = 4;
            private static readonly byte[] VerifyKey = new byte[VerifyKeyLength] { (byte)'P', (byte)'R', (byte)'J', (byte)'S' };

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = HeaderLength)]
            private readonly byte[] m_Header;

            private readonly byte m_Version;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = EncryptBytesLength)]
            private readonly byte[] m_EncryptBytes;

            private readonly int m_MaxFileCount;
            private readonly int m_MaxBlockCount;
            private readonly int m_BlockCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = VerifyKeyLength)]
            private readonly byte[] m_VerifyKey;

            public HeaderData(int maxFileCount, int maxBlockCount)
                : this(FileSystemVersion, new byte[EncryptBytesLength], maxFileCount, maxBlockCount, 0)
            {
                Utility.Random.GetRandomBytes(m_EncryptBytes);
            }

            public HeaderData(byte version, byte[] encryptBytes, int maxFileCount, int maxBlockCount, int blockCount)
            {
                m_Header = Header;
                m_Version = version;
                m_EncryptBytes = encryptBytes;
                m_MaxFileCount = maxFileCount;
                m_MaxBlockCount = maxBlockCount;
                m_BlockCount = blockCount;
                m_VerifyKey = VerifyKey;
            }

            public bool IsValid
            {
                get
                {
                    return m_Header.Length == HeaderLength && m_Header[0] == Header[0] && m_Header[1] == Header[1] && m_Header[2] == Header[2] && m_Version == FileSystemVersion && m_EncryptBytes.Length == EncryptBytesLength
                        && m_MaxFileCount > 0 && m_MaxBlockCount > 0 && m_MaxFileCount <= m_MaxBlockCount && m_BlockCount > 0 && m_BlockCount <= m_MaxBlockCount
                        && m_VerifyKey.Length == VerifyKeyLength && m_VerifyKey[0] == VerifyKey[0] && m_VerifyKey[1] == VerifyKey[1] && m_VerifyKey[2] == VerifyKey[2] && m_VerifyKey[3] == VerifyKey[3];
                }
            }

            public byte Version
            {
                get
                {
                    return m_Version;
                }
            }

            public int MaxFileCount
            {
                get
                {
                    return m_MaxFileCount;
                }
            }

            public int MaxBlockCount
            {
                get
                {
                    return m_MaxBlockCount;
                }
            }

            public int BlockCount
            {
                get
                {
                    return m_BlockCount;
                }
            }

            public byte[] GetEncryptBytes()
            {
                return m_EncryptBytes;
            }

            public HeaderData SetBlockCount(int blockCount)
            {
                return new HeaderData(m_Version, m_EncryptBytes, m_MaxFileCount, m_MaxBlockCount, blockCount);
            }
        }
    }
}
