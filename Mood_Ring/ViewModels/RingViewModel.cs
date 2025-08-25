// 프로젝트: Mood Ring 위젯
// 파일: RingViewModel.cs
// 설명: UI에 바인딩되는 핵심 상태(CompositeScore, 색상 Brush, 표시 텍스트) 관리.
//       MetricsService → SystemSnapshot → MoodService(점수/색) → ViewModel 업데이트.
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

    // 이모지 코드포인트 (직접 유니코드 표기: 일부 시스템에서 소스 파일 인코딩 문제 예방)
    private const string EmojiCalm = "\U0001F642";   // ?? Slightly Smiling Face (U+1F642)
    private const string EmojiNeutral = "\U0001F610"; // ?? Neutral Face (U+1F610)
    private const string EmojiEnergy = "\u26A1\uFE0F"; // ?? High Voltage + VS16 (색상 이모지 강제)
    private const string EmojiFire = "\U0001F525";    // ?? Fire (U+1F525)

    public Settings Settings => _settingsService.Current; // View에서 필요 시 직접 접근

    [ObservableProperty]
    private double compositeScore; // EMA 적용 후 최종 점수 (0~100)

    [ObservableProperty]
    private SolidColorBrush ringBrush = new SolidColorBrush(Colors.Teal); // 링 색상 브러시

    [ObservableProperty]
    private string displayText = "--"; // 중앙 표시 텍스트 (Score/Emoji/Text 모드)

    public RingViewModel(SettingsService ss, MetricsService ms, MoodService mood)
    {
        _settingsService = ss;
        _metricsService = ms;
        _moodService = mood;
    }

    // 주기적으로 호출하여 최신 상태 반영
    public void Update()
    {
        var snap = _metricsService.Sample();          // 현재 시스템 스냅샷 수집
        var score = _moodService.ComputeComposite(snap, Settings); // 종합 점수 + EMA
        CompositeScore = score;                       // 바인딩 갱신
        RingBrush = _moodService.GetBrush(score);     // 색상 브러시 업데이트
        DisplayText = Settings.DisplayMode switch     // 모드별 텍스트/이모지
        {
            "Emoji" => score switch { < 35 => EmojiCalm, <65 => EmojiNeutral, <85 => EmojiEnergy, _ => EmojiFire },
            "Text" => score switch { < 35 => "Calm", <65 => "Focus", <85 => "Busy", _ => "Heat" },
            _ => ((int)score).ToString()
        };
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
