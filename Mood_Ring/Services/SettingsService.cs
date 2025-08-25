// 프로젝트: Mood Ring 위젯
// 파일: SettingsService.cs
// 설명: 사용자 설정 로드/저장(JSON) 관리. 실패 시 조용히 기본값 유지하여 UX 저하 방지.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;
using System.IO;
using System.Text.Json;
using Mood_Ring.Models;

namespace Mood_Ring.Services;

public class SettingsService
{
    private readonly string _dir;   // 설정 디렉터리 (%AppData%\MoodRing)
    private readonly string _file;  // 설정 파일 경로
    public Settings Current { get; private set; } = new(); // 현재 메모리 내 설정 인스턴스

    public SettingsService()
    {
        _dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MoodRing");
        _file = Path.Combine(_dir, "settings.json");
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_file))
            {
                var json = File.ReadAllText(_file);
                var loaded = JsonSerializer.Deserialize<Settings>(json);
                if (loaded != null)
                    Current = loaded; // 역직렬화 성공 시 교체
            }
        }
        catch { /* 예외 무시: 파일 손상 등 - 기본값 사용 */ }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_dir);
            var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_file, json);
        }
        catch { /* 저장 실패도 무시 - 치명도 낮음 */ }
    }
}
