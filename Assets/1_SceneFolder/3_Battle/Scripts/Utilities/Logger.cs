using UnityEngine;

/// <summary>
/// 빌드 환경에 따라 로그 출력을 제어하는 경량 로거.
/// 에디터/개발 빌드에서는 Info/Warning 출력, 릴리즈 빌드에서는 Error만 출력한다.
/// </summary>
public static class Logger
{
    /// <summary>Info/Warning 로그 전체 on/off 스위치.</summary>
    public static bool EnableLogs = true;

    /// <summary>로그 상세 레벨.</summary>
    public enum LogLevel { Error = 0, Warning = 1, Info = 2 }
    public static LogLevel Level = LogLevel.Info;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static bool DevLoggingAllowed => EnableLogs;
#else
    // 릴리즈 빌드에서는 Info/Warning 출력 비활성화
    private static bool DevLoggingAllowed => false;
#endif

    /// <summary>Info 레벨 로그.</summary>
    public static void Log(string message, Object context = null)
    {
        if (DevLoggingAllowed && Level >= LogLevel.Info)
            Debug.Log(message, context);
    }

    /// <summary>Warning 레벨 로그.</summary>
    public static void LogWarning(string message, Object context = null)
    {
        if (DevLoggingAllowed && Level >= LogLevel.Warning)
            Debug.LogWarning(message, context);
    }

    /// <summary>Error 레벨 로그 (항상 출력).</summary>
    public static void LogError(string message, Object context = null)
    {
        Debug.LogError(message, context);
    }
}