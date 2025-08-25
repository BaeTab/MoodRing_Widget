// ������Ʈ: Mood Ring ����
// ����: SystemSnapshot.cs
// ����: Ư�� ������ �ý��� ���ҽ� ��� ���¸� ��� �Һ� DTO.
// �Ŀ�: �佺��ũ 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;

namespace Mood_Ring.Models;

public class SystemSnapshot
{
    public float CpuUsage { get; init; }       // CPU ���� % (0~100)
    public float MemoryUsage { get; init; }    // �޸� ���� % (�ǻ��/�ѿ뷮)
    public float DiskBusy { get; init; }       // ��ũ busy time %
    public float NetworkLoad { get; init; }    // ��Ʈ��ũ ��� ���� ������ (0~100, ���� �����ϸ�)
    public float BatteryPercent { get; init; } // ���͸� �ܷ� % (���� �߿��� �� ����)
    public bool IsCharging { get; init; }      // ���� ����/���� ����
}
