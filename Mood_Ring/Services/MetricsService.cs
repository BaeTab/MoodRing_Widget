// ������Ʈ: Mood Ring ����
// ����: MetricsService.cs
// ����: PerformanceCounter �� PowerStatus�� ����Ͽ� CPU/�޸�/��ũ/��Ʈ��ũ/���͸� ���� ���ø�.
//       ��Ʈ��ũ�� ���� �ִ�ġ �н� ��� ��� ����(0~100) �������� ���.
// ����: PerformanceCounter �ʱ� ȣ���� 0 �Ǵ� �̻�ġ ���� �� ù NextValue()�� ���־�.
// �Ŀ�: �佺��ũ 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms; // PowerStatus
using Mood_Ring.Models;

namespace Mood_Ring.Services;

public class MetricsService : IDisposable
{
    private PerformanceCounter? _cpu;          // CPU % Processor Time
    private PerformanceCounter? _disk;         // PhysicalDisk % Disk Time
    private List<PerformanceCounter> _net = new(); // NIC �� Bytes Total/sec
    private float _netScale = 1f;              // ��Ʈ��ũ ��� ������ (�ִ�ġ �н�)

    private bool _initialized;
    private DateTime _lastNetSample = DateTime.MinValue;
    private float _lastNetBytes;               // ���� �ջ� �� (rate ���)

    public void Init()
    {
        if (_initialized) return;
        try
        {
            _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            _ = _cpu.NextValue(); // ù ȣ�� ���� (�ǹ� ���� ��)
            _disk = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total", true);
            _ = _disk.NextValue();

            var cat = new PerformanceCounterCategory("Network Interface");
            foreach (var inst in cat.GetInstanceNames())
            {
                if (inst.ToLower().Contains("loopback")) continue; // ������ ����
                try
                {
                    _net.Add(new PerformanceCounter("Network Interface", "Bytes Total/sec", inst, true));
                }
                catch { /* �Ϻ� �ν��Ͻ� ���� ��� */ }
            }
            _lastNetBytes = SumNet(); // �ʱ� ��
            _lastNetSample = DateTime.UtcNow;
            _initialized = true;
        }
        catch
        {
            _initialized = false; // ���� �� Sample()���� ��õ� ����
        }
    }

    private float SumNet() => _net.Sum(n => SafeNext(n));

    private float SafeNext(PerformanceCounter c)
    {
        try { return c.NextValue(); } catch { return 0; }
    }

    public SystemSnapshot Sample()
    {
        if (!_initialized) Init();
        float cpu = 0, mem = 0, disk = 0, net = 0;

        // CPU ����
        if (_cpu != null) cpu = SafeNext(_cpu);

        // �޸�: (�� - ����) / �� * 100 (% ���)
        try
        {
            var ci = new Microsoft.VisualBasic.Devices.ComputerInfo();
            var total = (double)ci.TotalPhysicalMemory;
            var avail = (double)ci.AvailablePhysicalMemory;
            mem = (float)((total - avail) / total * 100.0);
        }
        catch { }

        // ��ũ busy
        if (_disk != null) disk = SafeNext(_disk);

        // ��Ʈ��ũ ��� ����: �ֱ� ������ ��� ������ �� 0~100
        try
        {
            var now = DateTime.UtcNow;
            var bytes = SumNet();
            if (_lastNetSample != DateTime.MinValue)
            {
                var dt = (float)(now - _lastNetSample).TotalSeconds;
                if (dt > 0.5f)
                {
                    var rate = (bytes - _lastNetBytes) / dt; // B/s
                    if (rate < 0) rate = 0;
                    if (rate > _netScale) _netScale = rate; // �ִ�ġ Ȯ��
                    else _netScale = _netScale * 0.995f + rate * 0.005f; // �ϸ� ����
                    if (_netScale < 1) _netScale = 1;
                    net = (float)Math.Min(100.0, rate / _netScale * 100.0);
                }
            }
            _lastNetBytes = bytes;
            _lastNetSample = now;
        }
        catch { }

        // ���͸� ����
        float battPct = 100;
        bool charging = false;
        try
        {
            var ps = SystemInformation.PowerStatus;
            battPct = ps.BatteryLifePercent * 100f;
            charging = ps.PowerLineStatus == PowerLineStatus.Online;
        }
        catch { }

        return new SystemSnapshot
        {
            CpuUsage = Clamp(cpu),
            MemoryUsage = Clamp(mem),
            DiskBusy = Clamp(disk),
            NetworkLoad = Clamp(net),
            BatteryPercent = Clamp(battPct),
            IsCharging = charging
        };
    }

    private static float Clamp(float v) => v < 0 ? 0 : (v > 100 ? 100 : v);

    public void Dispose()
    {
        try { _cpu?.Dispose(); } catch { }
        try { _disk?.Dispose(); } catch { }
        foreach (var n in _net) { try { n.Dispose(); } catch { } }
        _net.Clear();
        _initialized = false;
    }
}
