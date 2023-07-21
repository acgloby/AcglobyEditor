//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityGameFramework.Editor.ResourceTools
{
    public sealed partial class ResourceAnalyzerController
    {
        private readonly ResourceCollection m_ResourceCollection;

        private readonly Dictionary<string, DependencyData> m_DependencyDatas;//ResourceCollection.xml中每个Asset的依赖数据
        private readonly Dictionary<string, List<Asset>> m_ScatteredAssets;//游离资源，指不在ResourceCollection.xml中的资源，key=游离资源名，value=依赖该资源的Asset列表
        private readonly List<string[]> m_CircularDependencyDatas;//存在环路，互相依赖彼此的资源，这种资源不允许存在！！！
        private readonly HashSet<Stamp> m_AnalyzedStamps;

        public ResourceAnalyzerController()
            : this(null)
        {
        }

        public ResourceAnalyzerController(ResourceCollection resourceCollection)
        {
            m_ResourceCollection = resourceCollection != null ? resourceCollection : new ResourceCollection();

            m_ResourceCollection.OnLoadingResource += delegate (int index, int count)
            {
                if (OnLoadingResource != null)
                {
                    OnLoadingResource(index, count);
                }
            };

            m_ResourceCollection.OnLoadingAsset += delegate (int index, int count)
            {
                if (OnLoadingAsset != null)
                {
                    OnLoadingAsset(index, count);
                }
            };

            m_ResourceCollection.OnLoadCompleted += delegate ()
            {
                if (OnLoadCompleted != null)
                {
                    OnLoadCompleted();
                }
            };

            m_DependencyDatas = new Dictionary<string, DependencyData>(StringComparer.Ordinal);
            m_ScatteredAssets = new Dictionary<string, List<Asset>>(StringComparer.Ordinal);
            m_AnalyzedStamps = new HashSet<Stamp>();
            m_CircularDependencyDatas = new List<string[]>();
        }

        public event GameFrameworkAction<int, int> OnLoadingResource = null;

        public event GameFrameworkAction<int, int> OnLoadingAsset = null;

        public event GameFrameworkAction OnLoadCompleted = null;

        public event GameFrameworkAction<int, int> OnAnalyzingAsset = null;

        public event GameFrameworkAction OnAnalyzeCompleted = null;

        public void Clear()
        {
            m_ResourceCollection.Clear();
            m_DependencyDatas.Clear();
            m_ScatteredAssets.Clear();
            m_CircularDependencyDatas.Clear();
            m_AnalyzedStamps.Clear();
        }

        public bool Prepare()
        {
            m_ResourceCollection.Clear();
            return m_ResourceCollection.Load();
        }

        public void Analyze()
        {
            m_DependencyDatas.Clear();
            m_ScatteredAssets.Clear();
            m_CircularDependencyDatas.Clear();
            m_AnalyzedStamps.Clear();

            HashSet<string> scriptAssetNames = GetFilteredAssetNames("t:Script");
            Asset[] assets = m_ResourceCollection.GetAssets();
            int count = assets.Length;
            for (int i = 0; i < count; i++)
            {
                if (OnAnalyzingAsset != null)
                {
                    OnAnalyzingAsset(i, count);
                }

                string assetName = assets[i].Name;
                if (string.IsNullOrEmpty(assetName))
                {
                    Debug.LogWarning(Utility.Text.Format("Can not find asset by guid '{0}'.", assets[i].Guid));
                    continue;
                }

                DependencyData dependencyData = new DependencyData();
                AnalyzeAsset(assetName, assets[i], dependencyData, scriptAssetNames);
                dependencyData.RefreshData();
                m_DependencyDatas.Add(assetName, dependencyData);
            }

            foreach (List<Asset> scatteredAsset in m_ScatteredAssets.Values)
            {
                scatteredAsset.Sort((a, b) => a.Name.CompareTo(b.Name));
            }

            m_CircularDependencyDatas.AddRange(new CircularDependencyChecker(m_AnalyzedStamps.ToArray()).Check());

            if (OnAnalyzeCompleted != null)
            {
                OnAnalyzeCompleted();
            }
        }

        /// <summary>
        /// 递归分析依赖资源
        /// </summary>
        /// <param name="assetName">依赖资源名</param>
        /// <param name="hostAsset">依赖assetName的Asset，主资源</param>
        /// <param name="dependencyData">hostAsset的依赖资源数据</param>
        /// <param name="scriptAssetNames">工程内的代码脚本列表</param>
        private void AnalyzeAsset(string assetName, Asset hostAsset, DependencyData dependencyData, HashSet<string> scriptAssetNames)
        {
            string[] dependencyAssetNames = AssetDatabase.GetDependencies(assetName, false);
            foreach (string dependencyAssetName in dependencyAssetNames)
            {
                if (scriptAssetNames.Contains(dependencyAssetName))
                {
                    // 忽略对脚本的依赖
                    continue;
                }

                if (dependencyAssetName == assetName)
                {
                    // 忽略掉自己
                    continue;
                }

                if (dependencyAssetName.EndsWith(".unity", StringComparison.Ordinal))
                {
                    // 忽略对场景的依赖
                    continue;
                }

                if (dependencyAssetName.EndsWith(".cginc"))
                {
                    // 该资源只能在编辑器模式下使用，不能被打进AssetBundle里。
                    continue;
                }

                Stamp stamp = new Stamp(hostAsset.Name, dependencyAssetName);
                if (m_AnalyzedStamps.Contains(stamp))
                {
                    continue;
                }

                m_AnalyzedStamps.Add(stamp);

                string guid = AssetDatabase.AssetPathToGUID(dependencyAssetName);
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning(Utility.Text.Format("Can not find guid by asset '{0}'.", dependencyAssetName));
                    continue;
                }

                Asset asset = m_ResourceCollection.GetAsset(guid);
                if (asset != null)
                {
                    dependencyData.AddDependencyAsset(asset);
                }
                else
                {
                    dependencyData.AddScatteredDependencyAsset(dependencyAssetName);

                    List<Asset> scatteredAssets = null;
                    if (!m_ScatteredAssets.TryGetValue(dependencyAssetName, out scatteredAssets))
                    {
                        scatteredAssets = new List<Asset>();
                        m_ScatteredAssets.Add(dependencyAssetName, scatteredAssets);
                    }

                    scatteredAssets.Add(hostAsset);

                    AnalyzeAsset(dependencyAssetName, hostAsset, dependencyData, scriptAssetNames);
                }
            }
        }

        public Asset GetAsset(string assetName)
        {
            return m_ResourceCollection.GetAsset(AssetDatabase.AssetPathToGUID(assetName));
        }

        public string[] GetAssetNames()
        {
            return GetAssetNames(AssetsOrder.AssetNameAsc, null);
        }

        public string[] GetAssetNames(AssetsOrder order, string filter)
        {
            HashSet<string> filteredAssetNames = GetFilteredAssetNames(filter);
            IEnumerable<KeyValuePair<string, DependencyData>> filteredResult = m_DependencyDatas.Where(pair => filteredAssetNames.Contains(pair.Key));
            IEnumerable<KeyValuePair<string, DependencyData>> orderedResult = null;
            switch (order)
            {
                case AssetsOrder.AssetNameAsc:
                    orderedResult = filteredResult.OrderBy(pair => pair.Key);
                    break;

                case AssetsOrder.AssetNameDesc:
                    orderedResult = filteredResult.OrderByDescending(pair => pair.Key);
                    break;

                case AssetsOrder.DependencyResourceCountAsc:
                    orderedResult = filteredResult.OrderBy(pair => pair.Value.DependencyResourceCount);
                    break;

                case AssetsOrder.DependencyResourceCountDesc:
                    orderedResult = filteredResult.OrderByDescending(pair => pair.Value.DependencyResourceCount);
                    break;

                case AssetsOrder.DependencyAssetCountAsc:
                    orderedResult = filteredResult.OrderBy(pair => pair.Value.DependencyAssetCount);
                    break;

                case AssetsOrder.DependencyAssetCountDesc:
                    orderedResult = filteredResult.OrderByDescending(pair => pair.Value.DependencyAssetCount);
                    break;

                case AssetsOrder.ScatteredDependencyAssetCountAsc:
                    orderedResult = filteredResult.OrderBy(pair => pair.Value.ScatteredDependencyAssetCount);
                    break;

                case AssetsOrder.ScatteredDependencyAssetCountDesc:
                    orderedResult = filteredResult.OrderByDescending(pair => pair.Value.ScatteredDependencyAssetCount);
                    break;

                default:
                    orderedResult = filteredResult;
                    break;
            }

            return orderedResult.Select(pair => pair.Key).ToArray();
        }

        public DependencyData GetDependencyData(string assetName)
        {
            DependencyData dependencyData = null;
            if (m_DependencyDatas.TryGetValue(assetName, out dependencyData))
            {
                return dependencyData;
            }

            return dependencyData;
        }

        public string[] GetScatteredAssetNames()
        {
            return GetScatteredAssetNames(ScatteredAssetsOrder.HostAssetCountDesc, null);
        }

        public string[] GetScatteredAssetNames(ScatteredAssetsOrder order, string filter)
        {
            HashSet<string> filterAssetNames = GetFilteredAssetNames(filter);
            IEnumerable<KeyValuePair<string, List<Asset>>> filteredResult = m_ScatteredAssets.Where(pair => filterAssetNames.Contains(pair.Key) && pair.Value.Count > 1);
            IEnumerable<KeyValuePair<string, List<Asset>>> orderedResult = null;
            switch (order)
            {
                case ScatteredAssetsOrder.AssetNameAsc:
                    orderedResult = filteredResult.OrderBy(pair => pair.Key);
                    break;

                case ScatteredAssetsOrder.AssetNameDesc:
                    orderedResult = filteredResult.OrderByDescending(pair => pair.Key);
                    break;

                case ScatteredAssetsOrder.HostAssetCountAsc:
                    orderedResult = filteredResult.OrderBy(pair => pair.Value.Count);
                    break;

                case ScatteredAssetsOrder.HostAssetCountDesc:
                    orderedResult = filteredResult.OrderByDescending(pair => pair.Value.Count);
                    break;

                default:
                    orderedResult = filteredResult;
                    break;
            }

            return orderedResult.Select(pair => pair.Key).ToArray();
        }

        public Asset[] GetHostAssets(string scatteredAssetName)
        {
            List<Asset> assets = null;
            if (m_ScatteredAssets.TryGetValue(scatteredAssetName, out assets))
            {
                return assets.ToArray();
            }

            return null;
        }

        public string[][] GetCircularDependencyDatas()
        {
            return m_CircularDependencyDatas.ToArray();
        }

        private HashSet<string> GetFilteredAssetNames(string filter)
        {
            string[] filterAssetGuids = AssetDatabase.FindAssets(filter);
            HashSet<string> filterAssetNames = new HashSet<string>();
            foreach (string filterAssetGuid in filterAssetGuids)
            {
                filterAssetNames.Add(AssetDatabase.GUIDToAssetPath(filterAssetGuid));
            }

            return filterAssetNames;
        }
    }
}