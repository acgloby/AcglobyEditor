//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.Download;
using System;
using System.IO;

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        /// <summary>
        /// 版本资源列表处理器。
        /// </summary>
        private sealed class VersionListProcessor
        {
            private readonly ResourceManager m_ResourceManager;
            private IDownloadManager m_DownloadManager;
            private int m_VersionListLength;
            private int m_VersionListHashCode;
            private int m_VersionListCompressedLength;
            private int m_VersionListCompressedHashCode;

            public GameFrameworkAction<string, string> VersionListUpdateSuccess;
            public GameFrameworkAction<string, string> VersionListUpdateFailure;

            /// <summary>
            /// 初始化版本资源列表处理器的新实例。
            /// </summary>
            /// <param name="resourceManager">资源管理器。</param>
            public VersionListProcessor(ResourceManager resourceManager)
            {
                m_ResourceManager = resourceManager;
                m_DownloadManager = null;
                m_VersionListLength = 0;
                m_VersionListHashCode = 0;
                m_VersionListCompressedLength = 0;
                m_VersionListCompressedHashCode = 0;

                VersionListUpdateSuccess = null;
                VersionListUpdateFailure = null;
            }

            /// <summary>
            /// 关闭并清理版本资源列表处理器。
            /// </summary>
            public void Shutdown()
            {
                if (m_DownloadManager != null)
                {
                    m_DownloadManager.DownloadSuccess -= OnDownloadSuccess;
                    m_DownloadManager.DownloadFailure -= OnDownloadFailure;
                }
            }

            /// <summary>
            /// 设置下载管理器。
            /// </summary>
            /// <param name="downloadManager">下载管理器。</param>
            public void SetDownloadManager(IDownloadManager downloadManager)
            {
                if (downloadManager == null)
                {
                    throw new GameFrameworkException("Download manager is invalid.");
                }

                m_DownloadManager = downloadManager;
                m_DownloadManager.DownloadSuccess += OnDownloadSuccess;
                m_DownloadManager.DownloadFailure += OnDownloadFailure;
            }

            /// <summary>
            /// 检查版本资源列表。
            /// </summary>
            /// <param name="latestInternalResourceVersion">最新的内部资源版本号。</param>
            /// <returns>检查版本资源列表结果。</returns>
            public CheckVersionListResult CheckVersionList(int latestInternalResourceVersion)
            {
                if (string.IsNullOrEmpty(m_ResourceManager.m_ReadWritePath))
                {
                    throw new GameFrameworkException("Read-write path is invalid.");
                }

                string versionListFileName = Utility.Path.GetRegularPath(Path.Combine(m_ResourceManager.m_ReadWritePath, RemoteVersionListFileName));
                if (!File.Exists(versionListFileName))
                {
                    //本地还没有version文件 就把version更新下来 --by 王凯
                    return CheckVersionListResult.NeedUpdate;
                }

                int internalResourceVersion = 0;
                FileStream fileStream = null;
                try
                {
                    fileStream = new FileStream(versionListFileName, FileMode.Open, FileAccess.Read);
                    object internalResourceVersionObject = null;
                    if (!m_ResourceManager.m_UpdatableVersionListSerializer.TryGetValue(fileStream, "InternalResourceVersion", out internalResourceVersionObject))
                    {
                        return CheckVersionListResult.NeedUpdate;
                    }

                    internalResourceVersion = (int)internalResourceVersionObject;
                }
                catch
                {
                    return CheckVersionListResult.NeedUpdate;
                }
                finally
                {
                    if (fileStream != null)
                    {
                        fileStream.Dispose();
                        fileStream = null;
                    }
                }

                if (internalResourceVersion != latestInternalResourceVersion)
                {
                    return CheckVersionListResult.NeedUpdate;
                }

                return CheckVersionListResult.Updated;
            }

            /// <summary>
            /// 更新版本资源列表。
            /// </summary>
            /// <param name="versionListLength">版本资源列表大小。</param>
            /// <param name="versionListHashCode">版本资源列表哈希值。</param>
            /// <param name="versionListCompressedLength">版本资源列表压缩后大小。</param>
            /// <param name="versionListCompressedHashCode">版本资源列表压缩后哈希值。</param>
            public void UpdateVersionList(int versionListLength, int versionListHashCode, int versionListCompressedLength, int versionListCompressedHashCode)
            {
                if (m_DownloadManager == null)
                {
                    throw new GameFrameworkException("You must set download manager first.");
                }

                m_VersionListLength = versionListLength;
                m_VersionListHashCode = versionListHashCode;
                m_VersionListCompressedLength = versionListCompressedLength;
                m_VersionListCompressedHashCode = versionListCompressedHashCode;
                //下载完存放到读写区 这个路径localVersionListFilePath --by 王凯
                string localVersionListFilePath = Utility.Path.GetRegularPath(Path.Combine(m_ResourceManager.m_ReadWritePath, RemoteVersionListFileName));
                int dotPosition = RemoteVersionListFileName.LastIndexOf('.');
                string latestVersionListFullNameWithCrc32 = Utility.Text.Format("{0}.{2:x8}.{1}", RemoteVersionListFileName.Substring(0, dotPosition), RemoteVersionListFileName.Substring(dotPosition + 1), m_VersionListHashCode);
                //添加到下载列表 --by 王凯
                m_DownloadManager.AddDownload(localVersionListFilePath, Utility.Path.GetRemotePath(Path.Combine(m_ResourceManager.m_UpdatePrefixUri, latestVersionListFullNameWithCrc32)), this);
            }

            //下载version文件成功 GameFrameworkVersion.dat--by 王凯
            private void OnDownloadSuccess(object sender, DownloadSuccessEventArgs e)
            {
                //比对目录服上存的版本信息和真正下载到的GameFrameworkVersion.dat
                //其实每次游戏需要更新资源时 都会把打包生成的GameFrameworkVersion.dat的信息放在目录服上
                //这些信息包括：
                //versionListLength; 原文件的长度
                //versionListHashCode; 原文件的哈希值
                //versionListCompressedLength; 压缩后文件的长度
                //versionListCompressedHashCode; 压缩后文件的哈希值
                //用来和真正下载到的GameFrameworkVersion.dat进行比对
                //--by 王凯
                VersionListProcessor versionListProcessor = e.UserData as VersionListProcessor;
                if (versionListProcessor == null || versionListProcessor != this)
                {
                    return;
                }

                using (FileStream fileStream = new FileStream(e.DownloadPath, FileMode.Open, FileAccess.ReadWrite))
                {
                    int length = (int)fileStream.Length;
                    if (length != m_VersionListCompressedLength)
                    {
                        fileStream.Close();
                        string errorMessage = Utility.Text.Format("Latest version list compressed length error, need '{0}', downloaded '{1}'.", m_VersionListCompressedLength.ToString(), length.ToString());
                        DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                        OnDownloadFailure(this, downloadFailureEventArgs);
                        ReferencePool.Release(downloadFailureEventArgs);
                        return;
                    }

                    fileStream.Position = 0L;
                    int hashCode = Utility.Verifier.GetCrc32(fileStream);
                    if (hashCode != m_VersionListCompressedHashCode)
                    {
                        fileStream.Close();
                        string errorMessage = Utility.Text.Format("Latest version list compressed hash code error, need '{0}', downloaded '{1}'.", m_VersionListCompressedHashCode.ToString(), hashCode.ToString());
                        DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                        OnDownloadFailure(this, downloadFailureEventArgs);
                        ReferencePool.Release(downloadFailureEventArgs);
                        return;
                    }

                    if (m_ResourceManager.m_DecompressCachedStream == null)
                    {
                        m_ResourceManager.m_DecompressCachedStream = new MemoryStream();
                    }

                    try
                    {
                        fileStream.Position = 0L;
                        m_ResourceManager.m_DecompressCachedStream.Position = 0L;//清空m_ResourceManager.m_DecompressCachedStream  ---by 王凯
                        m_ResourceManager.m_DecompressCachedStream.SetLength(0L);//清空m_ResourceManager.m_DecompressCachedStream  ---by 王凯
                        //将下载的文件解压到m_ResourceManager.m_DecompressCachedStream  ---by 王凯
                        if (!Utility.Compression.Decompress(fileStream, m_ResourceManager.m_DecompressCachedStream))
                        {
                            fileStream.Close();
                            string errorMessage = Utility.Text.Format("Unable to decompress latest version list '{0}'.", e.DownloadPath);
                            DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                            OnDownloadFailure(this, downloadFailureEventArgs);
                            ReferencePool.Release(downloadFailureEventArgs);
                            return;
                        }

                        if (m_ResourceManager.m_DecompressCachedStream.Length != m_VersionListLength)
                        {
                            fileStream.Close();
                            string errorMessage = Utility.Text.Format("Latest version list length error, need '{0}', downloaded '{1}'.", m_VersionListLength.ToString(), m_ResourceManager.m_DecompressCachedStream.Length.ToString());
                            DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                            OnDownloadFailure(this, downloadFailureEventArgs);
                            ReferencePool.Release(downloadFailureEventArgs);
                            return;
                        }

                        //将解压后的数据流重新写入GameFrameworkVersion 这个文件已经下载到本地了 留着下一个流程 资源比对时用 压缩的没法用 所以这里有这个操作
                        //---by 王凯
                        fileStream.Position = 0L;//清空fileStream  ---by 王凯
                        fileStream.SetLength(0L);//清空fileStream  ---by 王凯
                        fileStream.Write(m_ResourceManager.m_DecompressCachedStream.GetBuffer(), 0, (int)m_ResourceManager.m_DecompressCachedStream.Length);
                    }
                    catch (Exception exception)
                    {
                        fileStream.Close();
                        string errorMessage = Utility.Text.Format("Unable to decompress latest version list '{0}' with error message '{1}'.", e.DownloadPath, exception.ToString());
                        DownloadFailureEventArgs downloadFailureEventArgs = DownloadFailureEventArgs.Create(e.SerialId, e.DownloadPath, e.DownloadUri, errorMessage, e.UserData);
                        OnDownloadFailure(this, downloadFailureEventArgs);
                        ReferencePool.Release(downloadFailureEventArgs);
                        return;
                    }
                    finally
                    {
                        m_ResourceManager.m_DecompressCachedStream.Position = 0L;
                        m_ResourceManager.m_DecompressCachedStream.SetLength(0L);
                    }
                }

                if (VersionListUpdateSuccess != null)
                {
                    VersionListUpdateSuccess(e.DownloadPath, e.DownloadUri);
                }
            }

            private void OnDownloadFailure(object sender, DownloadFailureEventArgs e)
            {
                VersionListProcessor versionListProcessor = e.UserData as VersionListProcessor;
                if (versionListProcessor == null || versionListProcessor != this)
                {
                    return;
                }

                if (File.Exists(e.DownloadPath))
                {
                    File.Delete(e.DownloadPath);
                }

                if (VersionListUpdateFailure != null)
                {
                    VersionListUpdateFailure(e.DownloadUri, e.ErrorMessage);
                }
            }
        }
    }
}
