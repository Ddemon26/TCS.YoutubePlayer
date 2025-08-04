using System.Diagnostics;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace TCS.YoutubePlayer.Utils {
    internal static class Logger {
        const string CLASS_COLOR = "cyan";
        const string CONTEXT_COLOR = "white";
        
        const string COLOR_LOG = "green";
        //const string TEXT_LOG ="LOG:";
        
        const string COLOR_WARNING = "yellow";
        const string TEXT_WARNING = "WARNING:";
        
        const string COLOR_ERROR = "red";
        const string TEXT_ERROR = "ERROR:";
        
        const string COLOR_ASSERT = "cyan";
        const string TEXT_ASSERT = "ASSERT:";
        
        const string COLOR_TODO = "orange";
        const string TEXT_TODO = "TODO:";

        static string GetCallerClassName() {
            var stackTrace = new StackTrace();
            
            for (int i = 2; i < stackTrace.FrameCount; i++) {
                var frame = stackTrace.GetFrame(i);
                var method = frame?.GetMethod();
                var declaringType = method?.DeclaringType;
                
                if (declaringType != null && declaringType != typeof(Logger)) {
                    string typeName = declaringType.Name;
                    
                    // Handle compiler-generated async method classes like <MethodName>d__X
                    if (typeName.StartsWith("<") && typeName.Contains(">d__")) {
                        // Try to get the actual declaring type (parent class)
                        var outerType = declaringType.DeclaringType;
                        if (outerType != null) {
                            return outerType.Name;
                        }
                    }
                    
                    // Handle other compiler-generated classes like <>c__DisplayClassX_Y
                    if (typeName.StartsWith("<>c__")) {
                        var outerType = declaringType.DeclaringType;
                        if (outerType != null) {
                            return outerType.Name;
                        }
                    }
                    
                    return typeName;
                }
            }
            
            return "Unknown";
        }

        static string SetLogPrefix(this object newString, LogType logType) {
            string color = logType switch {
                LogType.Log => COLOR_LOG,
                LogType.Warning => COLOR_WARNING,
                LogType.Error => COLOR_ERROR,
                LogType.Assert => COLOR_ASSERT,
                LogType.TODO => COLOR_TODO,
                _ => COLOR_LOG,
            };
            return $"<b><color={color}>{newString}</color></b>";
        }   
        
        static string SetClassPrefix(this object newString, LogType logType) {
            string color = logType switch {
                LogType.Warning => COLOR_WARNING,
                LogType.Error => COLOR_ERROR,
                LogType.TODO => COLOR_TODO,
                _ => CLASS_COLOR,
            };
            return $"<color=white><b>[</b><color={color}>{newString}</color><b>]</b></color>";
        }

        static string SetMessagePrefix(this object newString)   
            => $"<color={CONTEXT_COLOR}>{newString}</color>";

        static string FormatMessage(object message, LogType logType) {
            // [Classname/No color] [LogType/Color] [Message/No color]
            string type = logType switch {
                //LogType.Log => TEXT_LOG,
                LogType.Warning => TEXT_WARNING,
                LogType.Error => TEXT_ERROR,
                LogType.Assert => TEXT_ASSERT,
                LogType.TODO => TEXT_TODO,
                _ => "",
            };
            
            string className = GetCallerClassName();
            return $"{className.SetClassPrefix(logType)}" +
                   $"{type.SetLogPrefix(logType)}" +
                   $"{message.SetMessagePrefix()}";
        }

        static void LogInternal(object message, LogType logType, Object context = null) {
            
            string fixedMessage = FormatMessage(message, logType);
            
            switch (logType) {
                case LogType.Warning:
                    if (!context) {
                        Debug.LogWarning(fixedMessage);
                        break;
                    }
                    Debug.LogWarning(fixedMessage, context);
                    break;
                
                case LogType.Error:
                    if (!context) {
                        Debug.LogError(fixedMessage);
                        break;
                    }
                    Debug.LogError(fixedMessage, context);
                    break;
                
                case LogType.Assert:
                    if (!context) {
                        Debug.LogAssertion(fixedMessage);
                        break;
                    }
                    Debug.LogAssertion(fixedMessage, context);
                    break;
                
                case LogType.Exception:
                    string exception = message.SetMessagePrefix();
                    if (!context) {
                        Debug.LogException
                        (
                            new Exception(exception)
                        );
                        break;
                    }
                    Debug.LogException(new Exception(exception), context);
                    break;
                
                case LogType.TODO:
                case LogType.Log:
                default:
                    if (!context) {
                        Debug.Log(fixedMessage);
                        break;
                    }
                    Debug.Log(fixedMessage, context);
                    break;

            }
        }

        //Without context
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Log(object message) => LogInternal(message, LogType.Log);
        
        public static void LogWarning(object message) => LogInternal(message, LogType.Warning);
        public static void LogError(object message) => LogInternal(message, LogType.Error);
        public static void LogAssert(object message) => LogInternal(message, LogType.Assert);
        public static void LogException(object message) => LogInternal(message, LogType.Exception);
        
        [Conditional("UNITY_EDITOR")]
        public static void LogTODO(object message) => LogInternal(message, LogType.TODO);
        
        // Performance debugging
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void LogPerformance(string operation, TimeSpan duration) {
            LogInternal($"[PERF] {operation} took {duration.TotalMilliseconds:F2}ms", LogType.Log);
        }

        //With context
        public static void Log(object message, Object ctx) => LogInternal(message, LogType.Log, ctx);
        public static void LogWarning(object message, Object ctx) => LogInternal(message, LogType.Warning, ctx);
        public static void LogError(object message, Object ctx) => LogInternal(message, LogType.Error, ctx);
        public static void LogAssert(object message, Object ctx) => LogInternal(message, LogType.Assert, ctx);
        public static void LogException(object message, Object ctx) => LogInternal(message, LogType.Exception, ctx);
        public static void LogTODO(object message, Object ctx) => LogInternal(message, LogType.TODO, ctx);

        enum LogType {
            Log,
            Warning,
            Error,
            Assert,
            Exception,
            TODO,
        }
    }
}