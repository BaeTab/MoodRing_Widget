// 프로젝트: Mood Ring 위젯
// 파일: MoodService.cs
// 설명: 시스템 스냅샷 → 종합 점수(CompositeScore) 계산, EMA 스무딩, 점수→색상 매핑.
//       배터리 상태에 따른 점수 보정 적용.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using Mood_Ring.Helpers;
using Mood_Ring.Models;
using System.Windows.Media;

namespace Mood_Ring.Services;

public class MoodService
{
    private double? _ema; // EMA 누적값 (null이면 초기화 필요)

    // 점수 구간별 색상 스톱 (HSV 보간 대상). 색상 값: Tailwind 계열 청록→라임→주황→레드
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
        // 가중합 (각 메트릭은 0~100 범위)
        double score = s.CpuUsage * settings.CpuWeight +
                       s.MemoryUsage * settings.MemoryWeight +
                       s.DiskBusy * settings.DiskWeight +
                       s.NetworkLoad * settings.NetworkWeight;

        // 배터리 보정: 충전 중이면 -5, 20% 미만이면 +10 (과열/저전력 느낌)
        if (settings.BatteryAdjustment)
        {
            if (s.IsCharging) score -= 5;
            else if (s.BatteryPercent < 20) score += 10;
        }

        // 범위 클램프
        score = score < 0 ? 0 : (score > 100 ? 100 : score);

        // EMA 적용: ema = α*현재 + (1-α)*이전 (초기엔 현재값 채택)
        _ema = _ema is null ? score : settings.Alpha * score + (1 - settings.Alpha) * _ema.Value;
        return _ema.Value;
    }

    public SolidColorBrush GetBrush(double value)
    {
        // 구간 찾기 → HSV 보간 → Freezable Brush 반환
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
        b.Freeze(); // Freezable → 성능 최적화 (GC, 렌더 부하 감소)
        return b;
    }
}
