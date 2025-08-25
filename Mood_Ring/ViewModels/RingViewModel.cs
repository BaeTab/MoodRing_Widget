// ������Ʈ: Mood Ring ����
// ����: RingViewModel.cs
// ����: UI�� ���ε��Ǵ� �ٽ� ����(CompositeScore, ���� Brush, ǥ�� �ؽ�Ʈ) ����.
//       ������ �ΰ��� ����� ���� EMA ������ Raw ����(LastRawScore)�� ���.
// �Ŀ�: �佺��ũ 1001-2269-0600
// ----------------------------------------------------------------------------------
using CommunityToolkit.Mvvm.ComponentModel;
using Mood_Ring.Models;
using Mood_Ring.Services;
using System.Windows.Media;

namespace Mood_Ring.ViewModels;

public partial class RingViewModel : ObservableObject
{
    private readonly SettingsService _settingsService; // ����� ���� ����
    private readonly MetricsService _metricsService;   // �ý��� ��Ʈ�� ����
    private readonly MoodService _moodService;         // ���� ��� + ���� ����

    // �̸��� �ڵ�����Ʈ (���� �����ڵ� ǥ��)
    private const string EmojiCalm = "\U0001F642";   // ??
    private const string EmojiNeutral = "\U0001F610"; // ??
    private const string EmojiEnergy = "\u26A1\uFE0F"; // ??
    private const string EmojiFire = "\U0001F525";    // ??

    public Settings Settings => _settingsService.Current; // View���� �ʿ� �� ���� ����

    [ObservableProperty]
    private double compositeScore; // EMA ���� �� ���� ���� (0~100)

    [ObservableProperty]
    private SolidColorBrush ringBrush = new SolidColorBrush(Colors.Teal); // �� ���� �귯�� (Raw ���)

    [ObservableProperty]
    private string displayText = "--"; // �߾� ǥ�� �ؽ�Ʈ (Score/Emoji/Text ���)

    public RingViewModel(SettingsService ss, MetricsService ms, MoodService mood)
    {
        _settingsService = ss;
        _metricsService = ms;
        _moodService = mood;
    }

    // �ֱ� ������Ʈ: ��ġ�� �ε巴��, ������ �ﰢ ����
    public void Update()
    {
        var snap = _metricsService.Sample();
        var smoothed = _moodService.ComputeComposite(snap, Settings); // EMA ���� ��ġ
        CompositeScore = smoothed;                                    // ���� ǥ�� ����ȭ
        RingBrush = _moodService.GetBrush(_moodService.LastRawScore); // ������ raw ���� ��� (������ ��)
        var displayBase = Settings.DisplayMode switch
        {
            "Emoji" => _moodService.LastRawScore switch { < 35 => EmojiCalm, <65 => EmojiNeutral, <85 => EmojiEnergy, _ => EmojiFire },
            "Text"  => _moodService.LastRawScore switch { < 35 => "Calm", <65 => "Focus", <85 => "Busy", _ => "Heat" },
            _ => ((int)smoothed).ToString()
        };
        DisplayText = displayBase;
    }

    // ǥ�� ��� ��ȯ (Score �� Emoji �� Text �� Score)
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
