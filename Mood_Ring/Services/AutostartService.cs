// 프로젝트: Mood Ring 위젯
// 파일: AutostartService.cs
// 설명: 윈도우 시작 시 자동 실행 토글. HKCU\...\Run 레지스트리 값으로 제어(관리자 권한 불필요).
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;
using Microsoft.Win32;

namespace Mood_Ring.Services;

public static class AutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "MoodRing";

    // 현재 자동 실행 등록 여부
    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(AppName) != null;
        }
        catch { return false; }
    }

    // 자동 실행 등록/해제. 실패해도 앱 동작에는 영향 없음(설정 토글만 무시됨).
    public static bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKey);
            if (key == null) return false;

            if (enabled)
            {
                var exe = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exe)) return false;
                key.SetValue(AppName, $"\"{exe}\"");
            }
            else if (key.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }
            return true;
        }
        catch { return false; }
    }
}
