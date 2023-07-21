//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using UnityEditor;
using UnityEngine;

namespace UnityGameFramework.Editor.ResourceTools
{
    /// <summary>
    /// 生成资源。
    /// </summary>
    public static class BuildResources
    {
        /// <summary>
        /// 运行生成资源。
        /// </summary>
        [MenuItem("Game Framework/Resource Tools/Build Resources", false, 40)]
        public static void Run()
        {
            Run(null, Platform.Undefined, null, null);
        }

        public static void Run(int internalResourceVersion)
        {
            Run((int?)internalResourceVersion, Platform.Undefined, null, null);
        }

        public static void Run(Platform platforms)
        {
            Run(null, platforms, null, null);
        }

        public static void Run(string outputDirectory)
        {
            Run(null, Platform.Undefined, outputDirectory, null);
        }

        public static void Run(int internalResourceVersion, Platform platforms)
        {
            Run((int?)internalResourceVersion, platforms, null, null);
        }

        public static void Run(int internalResourceVersion, string outputDirectory)
        {
            Run((int?)internalResourceVersion, Platform.Undefined, outputDirectory, null);
        }

        public static void Run(Platform platforms, string outputDirectory)
        {
            Run(null, platforms, outputDirectory, null);
        }

        public static void Run(string outputDirectory, string buildEventHandlerTypeName)
        {
            Run(null, Platform.Undefined, outputDirectory, buildEventHandlerTypeName);
        }

        public static void Run(int internalResourceVersion, Platform platforms, string outputDirectory)
        {
            Run((int?)internalResourceVersion, platforms, outputDirectory, null);
        }

        public static void Run(int internalResourceVersion, string outputDirectory, string buildEventHandlerTypeName)
        {
            Run((int?)internalResourceVersion, Platform.Undefined, outputDirectory, buildEventHandlerTypeName);
        }

        public static void Run(Platform platforms, string outputDirectory, string buildEventHandlerTypeName)
        {
            Run(null, platforms, outputDirectory, buildEventHandlerTypeName);
        }

        public static void Run(int internalResourceVersion, string channelID, Platform platforms, string outputDirectory, string buildEventHandlerTypeName)
        {
            Run((int?)internalResourceVersion, platforms, outputDirectory, buildEventHandlerTypeName, channelID);
        }

        private static void Run(int? internalResourceVersion, Platform platforms, string outputDirectory, string buildEventHandlerTypeName, string channelID = "Test")
        {
            ResourceBuilderController controller = new ResourceBuilderController();
            if (!controller.Load())
            {
                throw new GameFrameworkException("Load configuration failure.");
            }
            else
            {
                Debug.Log("Load configuration success.");
            }

            controller.ChannelID = channelID;
            
            if (platforms != Platform.Undefined)
            {
                controller.Platforms = platforms;
            }

            if (internalResourceVersion.HasValue)
            {
                controller.InternalResourceVersion = internalResourceVersion.Value;
            }

            if (outputDirectory != null)
            {
                controller.OutputDirectory = outputDirectory;
            }

           
            if (buildEventHandlerTypeName != null)
            {
                controller.BuildEventHandlerTypeName = buildEventHandlerTypeName;
                //TODO 加上这句话，才有效!!看来之前想重写，后来又放弃了，半成品。目前还是在工具Resource Builder里控制。   by 李文国 2020-09-17 22:09
                //controller.RefreshBuildEventHandler();
            }

            if (!controller.IsValidOutputDirectory)
            {
                //Unity的bug，GameFrameworkException有时候输出的是log，不是Error， 额外输出一下，by 李文国 
                Debug.LogError(Utility.Text.Format("Output directory '{0}' is invalid.", controller.OutputDirectory));
                throw new GameFrameworkException(Utility.Text.Format("Output directory '{0}' is invalid.", controller.OutputDirectory));
            }

            if (!controller.BuildResources())
            {
                //Unity的bug，GameFrameworkException有时候输出的是log，不是Error， 额外输出一下，by 李文国 
                Debug.LogError("Build resources failure.");
                throw new GameFrameworkException("Build resources failure.");
            }
            else
            {
                Debug.Log("Build resources success.");
                controller.Save();
            }
        }
    }
}
