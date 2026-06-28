// 프로젝트: Mood Ring 위젯
// 파일: MetricsService.cs
// 설명: PerformanceCounter 및 PowerStatus를 사용하여 CPU/메모리/디스크/네트워크/배터리 상태 샘플링.
//       네트워크는 적응 최대치 학습 기반 상대 점수(0~100)로 정규화.
// 주의: PerformanceCounter 초기 호출은 0 또는 이상치를 낼 수 있어 첫 NextValue()를 버려줌.
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
    private List<PerformanceCounter> _net = new(); // NIC별 Bytes Total/sec
    private float _netScale = 1f;              // 네트워크 정규화 스케일 (최대치 학습)

    private volatile bool _initialized;
    private readonly object _gate = new(); // Init 동시 호출 직렬화
    private DateTime _lastNetSample = DateTime.MinValue;
    private float _lastNetBytes;               // 직전 합산 값 (rate 계산용)

    public void Init()
    {
        if (_initialized) return;
        lock (_gate)
        {
            if (_initialized) return;
            try
            {
                _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                _ = _cpu.NextValue(); // 첫 호출 워밍업 (의미 없는 값)
                _disk = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total", true);
                _ = _disk.NextValue();

                var cat = new PerformanceCounterCategory("Network Interface");
                foreach (var inst in cat.GetInstanceNames())
                {
                    if (inst.Contains("loopback", StringComparison.OrdinalIgnoreCase)) continue; // 루프백 제외
                    try
                    {
                        _net.Add(new PerformanceCounter("Network Interface", "Bytes Total/sec", inst, true));
                    }
                    catch { /* 일부 인스턴스 접근 실패 허용 */ }
                }
                _lastNetBytes = SumNet(); // 초기 값
                _lastNetSample = DateTime.UtcNow;
                _initialized = true;
            }
            catch
            {
                // 부분 초기화 상태 정리 (재시도 시 핸들 누수/카운터 중복 누적 방지)
                _cpu?.Dispose(); _cpu = null;
                _disk?.Dispose(); _disk = null;
                foreach (var n in _net) { try { n.Dispose(); } catch { } }
                _net.Clear();
                _initialized = false;
            }
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

        // 메모리: (총 - 가용) / 총 * 100
        try
        {
            var ci = new Microsoft.VisualBasic.Devices.ComputerInfo();
            var total = (double)ci.TotalPhysicalMemory;
            var avail = (double)ci.AvailablePhysicalMemory;
            mem = (float)((total - avail) / total * 100.0);
        }
        catch { mem = 50f; } // 실패 시 최선값(0) 대신 중립값으로 점수 왜곡 방지

        // 디스크 busy
        if (_disk != null) disk = SafeNext(_disk);

        // 네트워크 사용량: 최근 구간의 평균 전송률 → 0~100
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
                    else _netScale = _netScale * 0.995f + rate * 0.005f; // 서서히 하향
                    if (_netScale < 1) _netScale = 1;
                    net = (float)Math.Min(100.0, rate / _netScale * 100.0);
                }
            }
            _lastNetBytes = bytes;
            _lastNetSample = now;
        }
        catch { }

        // 배터리 상태
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
