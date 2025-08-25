// ������Ʈ: Mood Ring ����
// ����: SettingsService.cs
// ����: ����� ���� �ε�/����(JSON) ����. ���� �� ������ �⺻�� �����Ͽ� UX ���� ����.
// �Ŀ�: �佺��ũ 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;
using System.IO;
using System.Text.Json;
using Mood_Ring.Models;

namespace Mood_Ring.Services;

public class SettingsService
{
    private readonly string _dir;   // ���� ���͸� (%AppData%\MoodRing)
    private readonly string _file;  // ���� ���� ���
    public Settings Current { get; private set; } = new(); // ���� �޸� �� ���� �ν��Ͻ�

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
                    Current = loaded; // ������ȭ ���� �� ��ü
            }
        }
        catch { /* ���� ����: ���� �ջ� �� - �⺻�� ��� */ }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_dir);
            var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_file, json);
        }
        catch { /* ���� ���е� ���� - ġ�� ���� */ }
    }
}
