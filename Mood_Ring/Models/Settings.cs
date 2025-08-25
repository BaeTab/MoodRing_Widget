// ������Ʈ: Mood Ring ����
// ����: Settings.cs
// ����: ����� ����(����ġ, ������Ʈ ����, â ũ��/��ġ, ǥ�� ��� ��)�� ǥ��.
// �Ŀ�: �佺��ũ 1001-2269-0600 (���� ���� �����մϴ�!)
// ----------------------------------------------------------------------------------
using System.Text.Json.Serialization;

namespace Mood_Ring.Models;

public class Settings
{
    // ����ġ (���� 1�� �ǵ��� �⺻�� ����) - CPU 40%, �޸� 30%, ��ũ 15%, ��Ʈ��ũ 15%
    // �ʿ� �� ����� �������� ���� ���� (TODO: UI ���� �г�)
    public double CpuWeight { get; set; } = 0.40;   // CPU ���� �ݿ� ����
    public double MemoryWeight { get; set; } = 0.30; // �޸� ���� �ݿ� ����
    public double DiskWeight { get; set; } = 0.15;   // ��ũ Busy ����
    public double NetworkWeight { get; set; } = 0.15; // ��Ʈ��ũ ��� ���� ����

    public bool BatteryAdjustment { get; set; } = true; // ���͸� ���¿� ���� ����/���� ���� on/off
    public int UpdateIntervalMs { get; set; } = 1000;   // �ý��� ��Ʈ�� ���ø� �ֱ� (ms)

    public double Alpha { get; set; } = 0.2; // EMA(���� �̵� ���) ��� (0~1) - �������� �� �ε巯�� ��ȭ

    // â ũ��/��ġ (���� ����� ��� ���� ���� - ���� ȭ�� �� ���� ���� �߰� ����)
    public double WindowSize { get; set; } = 120; // �� ũ��(px)
    public double WindowLeft { get; set; } = 100; // ����� X ��ġ
    public double WindowTop { get; set; } = 100;  // ����� Y ��ġ

    // ǥ�� ���: ��ġ(Score) / �̸���(Emoji) / �ؽ�Ʈ(Text)
    public string DisplayMode { get; set; } = "Score"; 
    public bool Locked { get; set; } = false; // ��� �� �巡��/ũ�� ���� ��Ȱ��ȭ

    public bool AutoStart { get; set; } = false; // (TODO) ���� ���α׷� ��� ����
}
