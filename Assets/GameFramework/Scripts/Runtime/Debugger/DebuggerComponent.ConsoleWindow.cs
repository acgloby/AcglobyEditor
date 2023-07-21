//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.Debugger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : GameFrameworkComponent
    {
        [Serializable]
        private sealed class ConsoleWindow : IDebuggerWindow
        {
            private readonly Queue<LogNode> m_LogNodes = new Queue<LogNode>();
            private readonly TextEditor m_TextEditor = new TextEditor();

            private ConsoleWriter m_consoleWriter = new ConsoleWriter();
            private string m_logName = "DebugLog";

            private SettingComponent m_SettingComponent = null;
            private Vector2 m_LogScrollPosition = Vector2.zero;
            private Vector2 m_StackScrollPosition = Vector2.zero;
            private int m_InfoCount = 0;
            private int m_WarningCount = 0;
            private int m_ErrorCount = 0;
            private int m_FatalCount = 0;
            private LogNode m_SelectedNode = null;
            private bool m_LastLockScroll = true;
            private bool m_LastInfoFilter = true;
            private bool m_LastWarningFilter = true;
            private bool m_LastErrorFilter = true;
            private bool m_LastFatalFilter = true;

            [SerializeField]
            private bool m_LockScroll = true;

            [SerializeField]
            private int m_MaxLine = 100;

            [SerializeField]
            private bool m_InfoFilter = true;

            [SerializeField]
            private bool m_WarningFilter = true;

            [SerializeField]
            private bool m_ErrorFilter = true;

            [SerializeField]
            private bool m_FatalFilter = true;

            [SerializeField]
            private Color32 m_InfoColor = Color.white;

            [SerializeField]
            private Color32 m_WarningColor = Color.yellow;

            [SerializeField]
            private Color32 m_ErrorColor = Color.red;

            [SerializeField]
            private Color32 m_FatalColor = new Color(0.7f, 0.2f, 0.2f);

            public bool LockScroll
            {
                get
                {
                    return m_LockScroll;
                }
                set
                {
                    m_LockScroll = value;
                }
            }

            public int MaxLine
            {
                get
                {
                    return m_MaxLine;
                }
                set
                {
                    m_MaxLine = value;
                }
            }

            public bool InfoFilter
            {
                get
                {
                    return m_InfoFilter;
                }
                set
                {
                    m_InfoFilter = value;
                }
            }

            public bool WarningFilter
            {
                get
                {
                    return m_WarningFilter;
                }
                set
                {
                    m_WarningFilter = value;
                }
            }

            public bool ErrorFilter
            {
                get
                {
                    return m_ErrorFilter;
                }
                set
                {
                    m_ErrorFilter = value;
                }
            }

            public bool FatalFilter
            {
                get
                {
                    return m_FatalFilter;
                }
                set
                {
                    m_FatalFilter = value;
                }
            }

            public int InfoCount
            {
                get
                {
                    return m_InfoCount;
                }
            }

            public int WarningCount
            {
                get
                {
                    return m_WarningCount;
                }
            }

            public int ErrorCount
            {
                get
                {
                    return m_ErrorCount;
                }
            }

            public int FatalCount
            {
                get
                {
                    return m_FatalCount;
                }
            }

            public Color32 InfoColor
            {
                get
                {
                    return m_InfoColor;
                }
                set
                {
                    m_InfoColor = value;
                }
            }

            public Color32 WarningColor
            {
                get
                {
                    return m_WarningColor;
                }
                set
                {
                    m_WarningColor = value;
                }
            }

            public Color32 ErrorColor
            {
                get
                {
                    return m_ErrorColor;
                }
                set
                {
                    m_ErrorColor = value;
                }
            }

            public Color32 FatalColor
            {
                get
                {
                    return m_FatalColor;
                }
                set
                {
                    m_FatalColor = value;
                }
            }

            public void Initialize(params object[] args)
            {
                m_SettingComponent = GameEntry.GetComponent<SettingComponent>();
                if (m_SettingComponent == null)
                {
                    Log.Fatal("Setting component is invalid.");
                    return;
                }
                m_logName = string.Format("{0}_{1}", DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"), m_logName);
                m_consoleWriter.Initialize(m_logName);

                Application.logMessageReceived += OnLogMessageReceived;
                m_LockScroll = m_LastLockScroll = m_SettingComponent.GetBool("Debugger.Console.LockScroll", true);
                m_InfoFilter = m_LastInfoFilter = m_SettingComponent.GetBool("Debugger.Console.InfoFilter", true);
                m_WarningFilter = m_LastWarningFilter = m_SettingComponent.GetBool("Debugger.Console.WarningFilter", true);
                m_ErrorFilter = m_LastErrorFilter = m_SettingComponent.GetBool("Debugger.Console.ErrorFilter", true);
                m_FatalFilter = m_LastFatalFilter = m_SettingComponent.GetBool("Debugger.Console.FatalFilter", true);
            }

            public void Shutdown()
            {
                Application.logMessageReceived -= OnLogMessageReceived;
                Clear();
                m_consoleWriter.CloseWriter();
            }

            public void OnEnter()
            {
            }

            public void OnLeave()
            {
            }

            public void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                if (m_LastLockScroll != m_LockScroll)
                {
                    m_LastLockScroll = m_LockScroll;
                    m_SettingComponent.SetBool("Debugger.Console.LockScroll", m_LockScroll);
                }

                if (m_LastInfoFilter != m_InfoFilter)
                {
                    m_LastInfoFilter = m_InfoFilter;
                    m_SettingComponent.SetBool("Debugger.Console.InfoFilter", m_InfoFilter);
                }

                if (m_LastWarningFilter != m_WarningFilter)
                {
                    m_LastWarningFilter = m_WarningFilter;
                    m_SettingComponent.SetBool("Debugger.Console.WarningFilter", m_WarningFilter);
                }

                if (m_LastErrorFilter != m_ErrorFilter)
                {
                    m_LastErrorFilter = m_ErrorFilter;
                    m_SettingComponent.SetBool("Debugger.Console.ErrorFilter", m_ErrorFilter);
                }

                if (m_LastFatalFilter != m_FatalFilter)
                {
                    m_LastFatalFilter = m_FatalFilter;
                    m_SettingComponent.SetBool("Debugger.Console.FatalFilter", m_FatalFilter);
                }
            }

            public void OnDraw()
            {
                RefreshCount();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Clear All", GUILayout.Width(100f)))
                    {
                        Clear();
                    }
                    m_LockScroll = GUILayout.Toggle(m_LockScroll, "Lock Scroll", GUILayout.Width(90f));
                    GUILayout.FlexibleSpace();
                    m_InfoFilter = GUILayout.Toggle(m_InfoFilter, Utility.Text.Format("Info ({0})", m_InfoCount), GUILayout.Width(90f));
                    m_WarningFilter = GUILayout.Toggle(m_WarningFilter, Utility.Text.Format("Warning ({0})", m_WarningCount), GUILayout.Width(90f));
                    m_ErrorFilter = GUILayout.Toggle(m_ErrorFilter, Utility.Text.Format("Error ({0})", m_ErrorCount), GUILayout.Width(90f));
                    m_FatalFilter = GUILayout.Toggle(m_FatalFilter, Utility.Text.Format("Fatal ({0})", m_FatalCount), GUILayout.Width(90f));
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical("box");
                {
                    if (m_LockScroll)
                    {
                        m_LogScrollPosition.y = float.MaxValue;
                    }
                    // GUILayout.VerticalScrollbar
                    GUI.skin.verticalScrollbar.fixedWidth = 20;
                    GUI.skin.verticalScrollbarThumb.fixedWidth = 20;
                    GUI.skin.horizontalScrollbar.fixedWidth = 20;
                    GUI.skin.horizontalScrollbarThumb.fixedWidth = 20;
                    m_LogScrollPosition = GUILayout.BeginScrollView(m_LogScrollPosition);
                    {
                        bool selected = false;
                        foreach (LogNode logNode in m_LogNodes)
                        {
                            switch (logNode.LogType)
                            {
                                case LogType.Log:
                                    if (!m_InfoFilter)
                                    {
                                        continue;
                                    }
                                    break;

                                case LogType.Warning:
                                    if (!m_WarningFilter)
                                    {
                                        continue;
                                    }
                                    break;

                                case LogType.Error:
                                    if (!m_ErrorFilter)
                                    {
                                        continue;
                                    }
                                    break;

                                case LogType.Exception:
                                    if (!m_FatalFilter)
                                    {
                                        continue;
                                    }
                                    break;
                            }
                            if (GUILayout.Toggle(m_SelectedNode == logNode, GetLogString(logNode)))
                            {
                                selected = true;
                                if (m_SelectedNode != logNode)
                                {
                                    m_SelectedNode = logNode;
                                    m_StackScrollPosition = Vector2.zero;
                                }
                            }
                        }
                        if (!selected)
                        {
                            m_SelectedNode = null;
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                {
                    m_StackScrollPosition = GUILayout.BeginScrollView(m_StackScrollPosition, GUILayout.Height(100f));
                    {
                        if (m_SelectedNode != null)
                        {
                            GUILayout.BeginHorizontal();
                            Color32 color = GetLogStringColor(m_SelectedNode.LogType);
                            GUILayout.Label(Utility.Text.Format("<color=#{0}{1}{2}{3}><b>{4}</b></color>", color.r.ToString("x2"), color.g.ToString("x2"), color.b.ToString("x2"), color.a.ToString("x2"), m_SelectedNode.LogMessage));
                            if (GUILayout.Button("COPY", GUILayout.Width(60f), GUILayout.Height(30f)))
                            {
                                m_TextEditor.text = Utility.Text.Format("{0}{2}{2}{1}", m_SelectedNode.LogMessage, m_SelectedNode.StackTrack, Environment.NewLine);
                                m_TextEditor.OnFocus();
                                m_TextEditor.Copy();
                                m_TextEditor.text = null;
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.Label(m_SelectedNode.StackTrack);
                        }
                        GUILayout.EndScrollView();
                    }
                }
                GUILayout.EndVertical();
            }

            private void Clear()
            {
                m_LogNodes.Clear();
            }

            public void RefreshCount()
            {
                m_InfoCount = 0;
                m_WarningCount = 0;
                m_ErrorCount = 0;
                m_FatalCount = 0;
                foreach (LogNode logNode in m_LogNodes)
                {
                    switch (logNode.LogType)
                    {
                        case LogType.Log:
                            m_InfoCount++;
                            break;

                        case LogType.Warning:
                            m_WarningCount++;
                            break;

                        case LogType.Error:
                            m_ErrorCount++;
                            break;

                        case LogType.Exception:
                            m_FatalCount++;
                            break;
                    }
                }
            }

            public void GetRecentLogs(List<LogNode> results)
            {
                if (results == null)
                {
                    Log.Error("Results is invalid.");
                    return;
                }

                results.Clear();
                foreach (LogNode logNode in m_LogNodes)
                {
                    results.Add(logNode);
                }
            }

            public void GetRecentLogs(List<LogNode> results, int count)
            {
                if (results == null)
                {
                    Log.Error("Results is invalid.");
                    return;
                }

                if (count <= 0)
                {
                    Log.Error("Count is invalid.");
                    return;
                }

                int position = m_LogNodes.Count - count;
                if (position < 0)
                {
                    position = 0;
                }

                int index = 0;
                results.Clear();
                foreach (LogNode logNode in m_LogNodes)
                {
                    if (index++ < position)
                    {
                        continue;
                    }

                    results.Add(logNode);
                }
            }

            private void OnLogMessageReceived(string logMessage, string stackTrace, LogType logType)
            {
                if (logType == LogType.Assert)
                {
                    logType = LogType.Error;
                }

                LogNode node = LogNode.Create(logType, logMessage, stackTrace);
                m_LogNodes.Enqueue(node);
                m_consoleWriter.WriteLogInFile(node);
                while (m_LogNodes.Count > m_MaxLine)
                {
                    ReferencePool.Release(m_LogNodes.Dequeue());
                }
            }

            private string GetLogString(LogNode logNode)
            {
                Color32 color = GetLogStringColor(logNode.LogType);
                return Utility.Text.Format("<color=#{0}{1}{2}{3}>[{4}][{5}] {6}</color>",
                    color.r.ToString("x2"), color.g.ToString("x2"), color.b.ToString("x2"), color.a.ToString("x2"),
                    logNode.LogTime.ToString("HH:mm:ss.fff"), logNode.LogFrameCount.ToString(), logNode.LogMessage);
            }

            internal Color32 GetLogStringColor(LogType logType)
            {
                Color32 color = Color.white;
                switch (logType)
                {
                    case LogType.Log:
                        color = m_InfoColor;
                        break;

                    case LogType.Warning:
                        color = m_WarningColor;
                        break;

                    case LogType.Error:
                        color = m_ErrorColor;
                        break;

                    case LogType.Exception:
                        color = m_FatalColor;
                        break;
                }

                return color;
            }
        }

        private sealed class ConsoleWriter
        {
            private string m_logPath = "";
            private StringBuilder m_Content = new StringBuilder();
            private StreamWriter m_streamWriter = null;
            private string m_dirPath;

            public void Initialize(string logName)
            {
                GetLogPath(logName);
                CreateLogFile();
            }

            public void GetLogPath(string logName)
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                        {
                            m_logPath = string.Format("{0}/{1}.txt", Application.persistentDataPath, logName);
                            m_dirPath = Application.persistentDataPath;
                            Debug.LogWarning(string.Format("path:{0}", m_logPath));
                        }
                        break;

                    case RuntimePlatform.IPhonePlayer:
                        {
                            m_logPath = string.Format("{0}/{1}.txt", Application.persistentDataPath, logName);
                            m_dirPath = Application.persistentDataPath;
                        }
                        break;

                    case RuntimePlatform.WindowsEditor:
                        {
                            m_logPath = string.Format("{0}/../Reports/Debug/{1}.txt", Application.dataPath, logName);
                            m_dirPath = string.Format("{0}/../Reports/Debug", Application.dataPath);
                        }
                        break;

                    case RuntimePlatform.WindowsPlayer:
                        {
                            m_logPath = string.Format("{0}/../Reports/Debug/{1}.txt", Application.dataPath, logName);
                            m_dirPath = string.Format("{0}/../Reports/Debug", Application.dataPath);
                        }
                        break;
                }
            }

            public void CreateLogFile()
            {
                if (!System.IO.Directory.Exists(m_dirPath))
                    System.IO.Directory.CreateDirectory(m_dirPath);
                if (File.Exists(m_logPath))
                {
                    File.Delete(m_logPath);
                }

                m_streamWriter = new StreamWriter(m_logPath, true, Encoding.Default);
                m_streamWriter.AutoFlush = true;
            }

            public void WriteLogInFile(LogNode logNode)
            {
                m_Content.Clear();
                m_Content.Append("[").Append(logNode.LogTime.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append("][").Append(logNode.LogFrameCount.ToString()).Append("][").Append(logNode.LogType.ToString().ToUpper()).Append("] ").AppendLine(logNode.LogMessage);
                m_Content.AppendLine(logNode.StackTrack);
                m_streamWriter.Write(m_Content.ToString());
                m_streamWriter.Flush();
            }

            public void CloseWriter()
            {
                m_streamWriter.Close();
            }

            public void Dispose()
            {
                //  m_streamWriter.Dispose(false);
            }
        }
    }
}