// 프로젝트: Mood Ring 위젯
// 파일: MoodService.cs
// 설명: 시스템 스냅샷 → 종합 점수(CompositeScore) 계산, EMA 스무딩, 테마별 색상 매핑.
//       배터리 상태에 따른 가감점 포함. 색 반응성을 높이기 위해 Raw(원시) 점수도 노출.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Windows.Media;
using Mood_Ring.Helpers;
using Mood_Ring.Models;
using Color = System.Windows.Media.Color; // WinForms(System.Drawing.Color)와 모호성 제거
using ColorConverter = System.Windows.Media.ColorConverter;

namespace Mood_Ring.Services;

public class MoodService
{
    private double? _ema; // EMA 누적값 (null이면 초기화 필요)
    public double LastRawScore { get; private set; } // 최근 Raw(원시) 점수 (EMA 적용 전)

    // 사용 가능한 테마 이름 (설정 UI에서 노출)
    public static readonly string[] ThemeNames = { "Aurora", "Sunset", "Neon", "Ocean", "Mono" };

    // 테마별 색상 구간 정의 (pos 0~100 → hex). 낮음=차분, 높음=과열.
    private static readonly Dictionary<string, (double pos, string hex)[]> Palettes = new()
    {
        ["Aurora"] = new[] { (0.0, "#2dd4bf"), (25.0, "#2dd4bf"), (50.0, "#84cc16"), (75.0, "#fb923c"), (100.0, "#ef4444") },
        ["Sunset"] = new[] { (0.0, "#6366f1"), (35.0, "#a855f7"), (65.0, "#ec4899"), (85.0, "#f97316"), (100.0, "#fbbf24") },
        ["Neon"] = new[] { (0.0, "#22d3ee"), (35.0, "#34d399"), (65.0, "#facc15"), (85.0, "#fb7185"), (100.0, "#f0abfc") },
        ["Ocean"] = new[] { (0.0, "#0ea5e9"), (35.0, "#06b6d4"), (65.0, "#14b8a6"), (85.0, "#f59e0b"), (100.0, "#ef4444") },
        ["Mono"] = new[] { (0.0, "#94a3b8"), (35.0, "#cbd5e1"), (65.0, "#e2e8f0"), (85.0, "#fbbf24"), (100.0, "#f87171") },
    };

    // hex → Color 변환 결과 캐시 (테마별 1회만 파싱)
    private readonly Dictionary<string, (double pos, Color color)[]> _stopCache = new();

    public double ComputeComposite(SystemSnapshot s, Settings settings)
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

        // 클램프 후 Raw 점수 저장 (색/이모지 반응성 강화에 사용)
        raw = raw < 0 ? 0 : (raw > 100 ? 100 : raw);
        LastRawScore = raw;

        // EMA 스무딩 (수치 표시 안정화)
        _ema = _ema is null ? raw : settings.Alpha * raw + (1 - settings.Alpha) * _ema.Value;
        return _ema.Value;
    }

    // value(0~100)에 해당하는 테마 색상 반환 (HSV 최단거리 보간)
    public Color GetColor(double value, string theme = "Aurora")
    {
        var stops = GetStops(theme);
        if (value <= stops[0].pos) return stops[0].color;
        for (int i = 1; i < stops.Length; i++)
        {
            if (value <= stops[i].pos)
            {
                var (p1, c1) = stops[i - 1];
                var (p2, c2) = stops[i];
                double t = (value - p1) / (p2 - p1);
                return ColorInterpolation.LerpHsv(c1, c2, t);
            }
        }
        return stops[^1].color;
    }

    // value에 해당하는 Frozen SolidColorBrush (색이 바뀔 때만 새로 생성하여 GC 부담 최소화)
    private Color _lastBrushColor;
    private SolidColorBrush? _cachedBrush;
    public SolidColorBrush GetBrush(double value, string theme = "Aurora")
    {
        var c = GetColor(value, theme);
        if (_cachedBrush != null && c == _lastBrushColor) return _cachedBrush;
        _lastBrushColor = c;
        var b = new SolidColorBrush(c);
        b.Freeze();
        _cachedBrush = b;
        return b;
    }

    private (double pos, Color color)[] GetStops(string theme)
    {
        if (_stopCache.TryGetValue(theme, out var cached)) return cached;
        if (!Palettes.TryGetValue(theme, out var defs)) defs = Palettes["Aurora"];
        var arr = new (double pos, Color color)[defs.Length];
        for (int i = 0; i < defs.Length; i++)
            arr[i] = (defs[i].pos, (Color)ColorConverter.ConvertFromString(defs[i].hex)!);
        _stopCache[theme] = arr;
        return arr;
    }
}
