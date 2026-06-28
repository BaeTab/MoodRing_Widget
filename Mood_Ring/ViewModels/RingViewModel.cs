// 프로젝트: Mood Ring 위젯
// 파일: RingViewModel.cs
// 설명: UI에 바인딩되는 핵심 상태(CompositeScore, 링 색상, 표시 텍스트, 글로우 강도, 메트릭 상세).
//       색/이모지 반응성을 위해 EMA 이전 Raw 점수(LastRawScore)를 사용.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Mood_Ring.Models;
using Mood_Ring.Services;
using Color = System.Windows.Media.Color; // WinForms와 모호성 제거
using Point = System.Windows.Point;

namespace Mood_Ring.ViewModels;

public partial class RingViewModel : ObservableObject
{
    private readonly SettingsService _settingsService; // 사용자 설정 보관
    private readonly MetricsService _metricsService;   // 시스템 메트릭 수집
    private readonly MoodService _moodService;         // 점수 계산 + 색상 매핑

    // 이모지 코드포인트 (감성 단계 표현)
    private const string EmojiCalm = "\U0001F642";    // 🙂
    private const string EmojiNeutral = "\U0001F610"; // 😐
    private const string EmojiEnergy = "⚡";           // ⚡
    private const string EmojiFire = "\U0001F525";    // 🔥

    public Settings Settings => _settingsService.Current; // View에서 필요 시 직접 참조

    [ObservableProperty] private double compositeScore;          // EMA 적용 후 점수 (0~100)
    [ObservableProperty] private Color ringColor = Colors.Teal;  // 현재 링 색 (애니메이션 대상)
    [ObservableProperty] private SolidColorBrush ringBrush = new(Colors.Teal); // Brush 타입 바인딩용(스파크라인 등)
    [ObservableProperty] private string displayText = "--";      // 중앙 표시 텍스트
    [ObservableProperty] private string moodLabel = "Calm";      // 밴드 라벨
    [ObservableProperty] private double glowIntensity = 0.42;    // 글로우 불투명도(0.25~0.9)
    [ObservableProperty] private int moodBand;                   // 0 Calm /1 Focus /2 Busy /3 Heat

    // 상세 패널용 개별 메트릭 (0~100)
    [ObservableProperty] private double cpu;
    [ObservableProperty] private double memory;
    [ObservableProperty] private double disk;
    [ObservableProperty] private double network;
    [ObservableProperty] private double battery = 100;
    [ObservableProperty] private bool charging;

    // 스파크라인용 포인트 (최근 점수 추이)
    [ObservableProperty] private PointCollection sparklinePoints = new();
    private readonly Queue<double> _history = new();
    private const int HistoryMax = 48;
    private const double SparkW = 132, SparkH = 30, SparkPad = 2;

    public RingViewModel(SettingsService ss, MetricsService ms, MoodService mood)
    {
        _settingsService = ss;
        _metricsService = ms;
        _moodService = mood;
    }

    // 주기 업데이트: 수치는 부드럽게(EMA), 색/이모지는 즉각 반응(Raw)
    public void Update()
    {
        var snap = _metricsService.Sample();
        var smoothed = _moodService.ComputeComposite(snap, Settings); // EMA 점수
        double raw = _moodService.LastRawScore;

        CompositeScore = smoothed;

        // 개별 메트릭 노출 (상세 패널 바인딩)
        Cpu = snap.CpuUsage;
        Memory = snap.MemoryUsage;
        Disk = snap.DiskBusy;
        Network = snap.NetworkLoad;
        Battery = snap.BatteryPercent;
        Charging = snap.IsCharging;

        // 색상 (테마 반영, Raw 기반으로 민감하게)
        RingColor = _moodService.GetColor(raw, Settings.ColorTheme);
        RingBrush = _moodService.GetBrush(raw, Settings.ColorTheme);

        // 밴드/라벨
        int band = raw < 35 ? 0 : raw < 65 ? 1 : raw < 85 ? 2 : 3;
        MoodBand = band;
        MoodLabel = band switch { 0 => "Calm", 1 => "Focus", 2 => "Busy", _ => "Heat" };

        // 글로우 강도 (옵션 켜졌을 때만 점수 반영)
        GlowIntensity = Settings.ReactiveGlow ? 0.30 + raw / 100.0 * 0.55 : 0.42;

        // 중앙 표시 텍스트
        DisplayText = Settings.DisplayMode switch
        {
            "Emoji" => band switch { 0 => EmojiCalm, 1 => EmojiNeutral, 2 => EmojiEnergy, _ => EmojiFire },
            "Text" => MoodLabel,
            _ => ((int)smoothed).ToString()
        };

        PushHistory(smoothed);
    }

    private void PushHistory(double value)
    {
        _history.Enqueue(value);
        while (_history.Count > HistoryMax) _history.Dequeue();
        RebuildSparkline();
    }

    private void RebuildSparkline()
    {
        var pts = new PointCollection();
        if (_history.Count >= 2)
        {
            var arr = _history.ToArray();
            int n = arr.Length;
            for (int i = 0; i < n; i++)
            {
                double x = SparkPad + (SparkW - 2 * SparkPad) * i / (n - 1);
                double y = SparkPad + (SparkH - 2 * SparkPad) * (1 - arr[i] / 100.0);
                pts.Add(new Point(x, y));
            }
        }
        pts.Freeze();
        SparklinePoints = pts;
    }

    // 표시 모드 순환 (Score → Emoji → Text → Score)
    public void ToggleDisplayMode()
    {
        Settings.DisplayMode = Settings.DisplayMode switch
        {
            "Score" => "Emoji",
            "Emoji" => "Text",
            _ => "Score"
        };
    }
}
