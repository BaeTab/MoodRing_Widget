// 프로젝트: Mood Ring 위젯
// 파일: MoodService.cs
// 설명: 시스템 스냅샷 → 종합 점수(CompositeScore) 계산, EMA 스무딩, 점수→색상 매핑.
//       배터리 상태에 따른 점수 보정 적용. 색상 반응성을 높이기 위해 Raw(즉시) 점수도 보관.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using Mood_Ring.Helpers;
using Mood_Ring.Models;
using System.Windows.Media;

namespace Mood_Ring.Services;

public class MoodService
{
    private double? _ema;                 // EMA 누적값 (null이면 초기화 필요)
    public double LastRawScore { get; private set; } // 최근 계산된 즉시(raw) 점수 (보정+클램프 후, EMA 적용 전)

    // 점수 구간별 색상 스톱 (HSV 보간 대상)
    private readonly (double pos, System.Windows.Media.Color color)[] _stops = new[]
    {
        (0.0,  (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2dd4bf")!),
        (25.0, (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2dd4bf")!),
        (50.0, (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#84cc16")!),
        (75.0, (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#fb923c")!),
        (100.0,(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ef4444")!)
    };

    public double ComputeComposite(SystemSnapshot s, Models.Settings settings)
    {
        // 가중합 (각 메트릭 0~100)
        double raw = s.CpuUsage * settings.CpuWeight +
                     s.MemoryUsage * settings.MemoryWeight +
                     s.DiskBusy * settings.DiskWeight +
                     s.NetworkLoad * settings.NetworkWeight;

        // 배터리 보정
        if (settings.BatteryAdjustment)
        {
            if (s.IsCharging) raw -= 5;
            else if (s.BatteryPercent < 20) raw += 10;
        }

        // 클램프 → Raw 점수 저장 (색상 즉시 반응에 사용)
        raw = raw < 0 ? 0 : (raw > 100 ? 100 : raw);
        LastRawScore = raw; // 색상은 EMA 대신 이 값을 사용하여 민감하게 반응

        // EMA 적용으로 수치 표기 안정화 (시각적 점수 숫자 흔들림 감소)
        _ema = _ema is null ? raw : settings.Alpha * raw + (1 - settings.Alpha) * _ema.Value;
        return _ema.Value; // 반환값은 "스무딩 점수" (CompositeScore)
    }

    // value(0~100)에 대한 HSV 보간 브러시 (즉시 점수 전달 권장)
    public SolidColorBrush GetBrush(double value)
    {
        if (value <= _stops[0].pos) return Frozen(_stops[0].color);
        for (int i = 1; i < _stops.Length; i++)
        {
            if (value <= _stops[i].pos)
            {
                var (p1, c1) = _stops[i - 1];
                var (p2, c2) = _stops[i];
                double t = (value - p1) / (p2 - p1);
                var c = ColorInterpolation.LerpHsv(c1, c2, t);
                return Frozen(c);
            }
        }
        return Frozen(_stops[^1].color);
    }

    private SolidColorBrush Frozen(System.Windows.Media.Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze(); // Freezable → 성능 최적화
        return b;
    }
}
