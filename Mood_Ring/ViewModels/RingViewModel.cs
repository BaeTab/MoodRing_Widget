// 프로젝트: Mood Ring 위젯
// 파일: RingViewModel.cs
// 설명: UI에 바인딩되는 핵심 상태(CompositeScore, 색상 Brush, 표시 텍스트) 관리.
//       색상은 민감도 향상을 위해 EMA 이전의 Raw 점수(LastRawScore)를 사용.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using CommunityToolkit.Mvvm.ComponentModel;
using Mood_Ring.Models;
using Mood_Ring.Services;
using System.Windows.Media;

namespace Mood_Ring.ViewModels;

public partial class RingViewModel : ObservableObject
{
    private readonly SettingsService _settingsService; // 사용자 설정 접근
    private readonly MetricsService _metricsService;   // 시스템 메트릭 수집
    private readonly MoodService _moodService;         // 점수 계산 + 색상 매핑

    // 이모지 코드포인트 (직접 유니코드 표기)
    private const string EmojiCalm = "\U0001F642";   // ??
    private const string EmojiNeutral = "\U0001F610"; // ??
    private const string EmojiEnergy = "\u26A1\uFE0F"; // ??
    private const string EmojiFire = "\U0001F525";    // ??

    public Settings Settings => _settingsService.Current; // View에서 필요 시 직접 접근

    [ObservableProperty]
    private double compositeScore; // EMA 적용 후 최종 점수 (0~100)

    [ObservableProperty]
    private SolidColorBrush ringBrush = new SolidColorBrush(Colors.Teal); // 링 색상 브러시 (Raw 기반)

    [ObservableProperty]
    private string displayText = "--"; // 중앙 표시 텍스트 (Score/Emoji/Text 모드)

    public RingViewModel(SettingsService ss, MetricsService ms, MoodService mood)
    {
        _settingsService = ss;
        _metricsService = ms;
        _moodService = mood;
    }

    // 주기 업데이트: 수치는 부드럽게, 색상은 즉각 반응
    public void Update()
    {
        var snap = _metricsService.Sample();
        var smoothed = _moodService.ComputeComposite(snap, Settings); // EMA 적용 수치
        CompositeScore = smoothed;                                    // 숫자 표기 안정화
        RingBrush = _moodService.GetBrush(_moodService.LastRawScore); // 색상은 raw 점수 사용 (반응성 ↑)
        var displayBase = Settings.DisplayMode switch
        {
            "Emoji" => _moodService.LastRawScore switch { < 35 => EmojiCalm, <65 => EmojiNeutral, <85 => EmojiEnergy, _ => EmojiFire },
            "Text"  => _moodService.LastRawScore switch { < 35 => "Calm", <65 => "Focus", <85 => "Busy", _ => "Heat" },
            _ => ((int)smoothed).ToString()
        };
        DisplayText = displayBase;
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
