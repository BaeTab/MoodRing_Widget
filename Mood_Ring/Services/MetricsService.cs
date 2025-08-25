// 프로젝트: Mood Ring 위젯
// 파일: MetricsService.cs
// 설명: PerformanceCounter 및 PowerStatus를 사용하여 CPU/메모리/디스크/네트워크/배터리 상태 샘플링.
//       네트워크는 동적 최대치 학습 기반 상대 부하(0~100) 스케일을 사용.
// 주의: PerformanceCounter 초기 호출은 0 또는 이상치 가능 → 첫 NextValue()로 워밍업.
// 후원: 토스뱅크 1001-2269-0600
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
    private List<PerformanceCounter> _net = new(); // NIC 별 Bytes Total/sec
    private float _netScale = 1f;              // 네트워크 상대 스케일 (최대치 학습)

    private bool _initialized;
    private DateTime _lastNetSample = DateTime.MinValue;
    private float _lastNetBytes;               // 이전 합산 값 (rate 계산)

    public void Init()
    {
        if (_initialized) return;
        try
        {
            _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            _ = _cpu.NextValue(); // 첫 호출 웜업 (의미 없는 값)
            _disk = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total", true);
            _ = _disk.NextValue();

            var cat = new PerformanceCounterCategory("Network Interface");
            foreach (var inst in cat.GetInstanceNames())
            {
                if (inst.ToLower().Contains("loopback")) continue; // 루프백 제외
                try
                {
                    _net.Add(new PerformanceCounter("Network Interface", "Bytes Total/sec", inst, true));
                }
                catch { /* 일부 인스턴스 실패 허용 */ }
            }
            _lastNetBytes = SumNet(); // 초기 합
            _lastNetSample = DateTime.UtcNow;
            _initialized = true;
        }
        catch
        {
            _initialized = false; // 실패 시 Sample()에서 재시도 가능
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

        // CPU 사용률
        if (_cpu != null) cpu = SafeNext(_cpu);

        // 메모리: (총 - 가용) / 총 * 100 (% 사용)
        try
        {
            var ci = new Microsoft.VisualBasic.Devices.ComputerInfo();
            var total = (double)ci.TotalPhysicalMemory;
            var avail = (double)ci.AvailablePhysicalMemory;
            mem = (float)((total - avail) / total * 100.0);
        }
        catch { }

        // 디스크 busy
        if (_disk != null) disk = SafeNext(_disk);

        // 네트워크 상대 부하: 최근 관측률 기반 스케일 → 0~100
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
                    if (rate > _netScale) _netScale = rate; // 최대치 확장
                    else _netScale = _netScale * 0.995f + rate * 0.005f; // 완만 감소
                    if (_netScale < 1) _netScale = 1;
                    net = (float)Math.Min(100.0, rate / _netScale * 100.0);
                }
            }
            _lastNetBytes = bytes;
            _lastNetSample = now;
        }
        catch { }

        // 배터리 정보
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
