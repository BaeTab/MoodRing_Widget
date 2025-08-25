// ������Ʈ: Mood Ring ����
// ����: RingViewModel.cs
// ����: UI�� ���ε��Ǵ� �ٽ� ����(CompositeScore, ���� Brush, ǥ�� �ؽ�Ʈ) ����.
//       MetricsService �� SystemSnapshot �� MoodService(����/��) �� ViewModel ������Ʈ.
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

    // �̸��� �ڵ�����Ʈ (���� �����ڵ� ǥ��: �Ϻ� �ý��ۿ��� �ҽ� ���� ���ڵ� ���� ����)
    private const string EmojiCalm = "\U0001F642";   // ?? Slightly Smiling Face (U+1F642)
    private const string EmojiNeutral = "\U0001F610"; // ?? Neutral Face (U+1F610)
    private const string EmojiEnergy = "\u26A1\uFE0F"; // ?? High Voltage + VS16 (���� �̸��� ����)
    private const string EmojiFire = "\U0001F525";    // ?? Fire (U+1F525)

    public Settings Settings => _settingsService.Current; // View���� �ʿ� �� ���� ����

    [ObservableProperty]
    private double compositeScore; // EMA ���� �� ���� ���� (0~100)

    [ObservableProperty]
    private SolidColorBrush ringBrush = new SolidColorBrush(Colors.Teal); // �� ���� �귯��

    [ObservableProperty]
    private string displayText = "--"; // �߾� ǥ�� �ؽ�Ʈ (Score/Emoji/Text ���)

    public RingViewModel(SettingsService ss, MetricsService ms, MoodService mood)
    {
        _settingsService = ss;
        _metricsService = ms;
        _moodService = mood;
    }

    // �ֱ������� ȣ���Ͽ� �ֽ� ���� �ݿ�
    public void Update()
    {
        var snap = _metricsService.Sample();          // ���� �ý��� ������ ����
        var score = _moodService.ComputeComposite(snap, Settings); // ���� ���� + EMA
        CompositeScore = score;                       // ���ε� ����
        RingBrush = _moodService.GetBrush(score);     // ���� �귯�� ������Ʈ
        DisplayText = Settings.DisplayMode switch     // ��庰 �ؽ�Ʈ/�̸���
        {
            "Emoji" => score switch { < 35 => EmojiCalm, <65 => EmojiNeutral, <85 => EmojiEnergy, _ => EmojiFire },
            "Text" => score switch { < 35 => "Calm", <65 => "Focus", <85 => "Busy", _ => "Heat" },
            _ => ((int)score).ToString()
        };
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
