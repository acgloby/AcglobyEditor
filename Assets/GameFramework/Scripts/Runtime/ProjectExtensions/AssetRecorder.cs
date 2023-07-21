using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public static class AssetRecorder
    {
        private static readonly HashSet<string> s_AssetNames = new HashSet<string>();

        public static void Clear()
        {
            s_AssetNames.Clear();
        }

        public static void Record(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return;
            }

            if (!assetName.StartsWith("Assets/"))
            {
                return;
            }

            if (assetName.EndsWith(".cs"))
            {
                return;
            }

            s_AssetNames.Add(assetName);
        }

        public static void Save()
        {
            List<string> assetNames = new List<string>(s_AssetNames);
            assetNames.Sort();
            string path = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length) + "Reports/AssetNameCollection.txt";
            File.WriteAllLines(path, assetNames);
        }
    }
}
