using Microsoft.International.Converters.PinYinConverter;
using System.Collections.Generic;
using System.Diagnostics;

namespace GameFramework
{
    public static partial class Utility
    {
        /// <summary>
        /// 调试相关的实用函数。
        /// </summary>
        public static class Profiler
        {
            public const string ProfilerOnlyLabel = "[Profiler Only]";

            [Conditional("ENABLE_PS_PROFILER")]
            public static void BeginSample(string name)
            {
                UnityEngine.Profiling.Profiler.BeginSample(name);
                //UWAEngine.PushSample(name);
            }

            [Conditional("ENABLE_PS_PROFILER")]
            public static void EndSample()
            {


                UnityEngine.Profiling.Profiler.EndSample();
            }

            public static string Format(string format, object arg0)
            {
#if ENABLE_PS_PROFILER && ENABLE_PROFILER
                Utility.Profiler.BeginSample(ProfilerOnlyLabel);
                string str = Text.Format(format, arg0);
                Utility.Profiler.EndSample();
                return str;
#else
                return null;
#endif
            }

            public static string Format(string format, object arg0, object arg1)
            {
#if ENABLE_PS_PROFILER && ENABLE_PROFILER
                Utility.Profiler.BeginSample(ProfilerOnlyLabel);
                string str = Text.Format(format, arg0, arg1);
                Utility.Profiler.EndSample();
                return str;
#else
                return null;
#endif
            }

            public static string Format(string format, object arg0, object arg1, object arg2)
            {
#if ENABLE_PS_PROFILER && ENABLE_PROFILER
                Utility.Profiler.BeginSample(ProfilerOnlyLabel);
                string str = Text.Format(format, arg0, arg1, arg2);
                Utility.Profiler.EndSample();
                return str;
#else
                return null;
#endif
            }

            public static string Format(string format, params object[] args)
            {
#if ENABLE_PS_PROFILER && ENABLE_PROFILER
                Utility.Profiler.BeginSample(ProfilerOnlyLabel);
                string str = Text.Format(format, args);
                Utility.Profiler.EndSample();
                return str;
#else
                return null;
#endif
            }

            private static readonly Dictionary<string, string> s_StrToPinYin = new Dictionary<string, string>();

            public static string ConvertToPinYin(string str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    return str;
                }

                string result = null;
                if (s_StrToPinYin.TryGetValue(str, out result))
                {
                    return result;
                }

                result = string.Empty;
                foreach (char ch in str.ToCharArray())
                {
                    result += ChineseChar.IsValidChar(ch) ? new ChineseChar(ch).Pinyins[0] : ch.ToString();
                }

                s_StrToPinYin.Add(str, result);
                return result;
            }
        }
    }
}
