// ������Ʈ: Mood Ring ����
// ����: RingWindow.xaml.cs
// ����: �����Ӹ��� ���� TopMost ���� â. �巡�� �̵�, ũ�� ����(��), ����Ŭ�� ��� ���,
//       �ڵ� ���̵�(���콺 �Ÿ� ���), Ʈ���� ���� �ʱ�ȭ �� �ֱ��� ������Ʈ ����.
// �Ŀ�: �佺��ũ 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Mood_Ring.Services;
using Mood_Ring.ViewModels;

namespace Mood_Ring.Views;

public partial class RingWindow : Window
{
    private readonly SettingsService _settingsService = new(); // ����� ����
    private readonly MetricsService _metricsService = new();     // ��Ʈ�� ����
    private readonly MoodService _moodService = new();           // ����/�� ���
    private readonly RingViewModel _vm;                          // ViewModel �ν��Ͻ�
    private readonly TrayService _tray;                          // Ʈ���� ������
    private readonly DispatcherTimer _timer;                     // ��Ʈ�� ������Ʈ Ÿ�̸�
    private readonly DispatcherTimer _fadeTimer;                 // �ڵ� ���� Ÿ�̸�

    private double _targetOpacity = 1.0;                         // ���̵� ��ǥ�� (������ ��ȭ ���� �ּ� 0.85)

    public RingWindow()
    {
        InitializeComponent();
        _vm = new RingViewModel(_settingsService, _metricsService, _moodService);
        DataContext = _vm;

        // ��ġ/ũ�� ����
        Width = Height = _settingsService.Current.WindowSize;
        Left = _settingsService.Current.WindowLeft;
        Top = _settingsService.Current.WindowTop;

        // Ʈ���� �ʱ�ȭ
        _tray = new TrayService(_settingsService, ToggleVisibility, SetSize, SaveSettings, ToggleLock);
        _tray.Init();

        // �ֱ� ������Ʈ Ÿ�̸� (CompositeScore/����)
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_settingsService.Current.UpdateIntervalMs) };
        _timer.Tick += (_, __) => _vm.Update();
        _timer.Start();
        Loaded += (_, __) => _vm.Update(); // ���� ��� �� ��
        Closed += (_, __) => OnClosedInternal();
        MouseLeftButtonUp += (_, __) => { if (!SettingsLocked) { SaveSettings(); } }; // �巡�� ���� �� ��ġ ����
        MouseDown += (_, e) => { if (e.ClickCount == 2) { _vm.ToggleDisplayMode(); SaveSettings(); } }; // ����Ŭ�� ǥ�� ��� ��ȯ

        // �ڵ� ���̵� (���콺 ����=1.0 / ���Ÿ�=0.85) - 500ms �ֱ� ���� ����
        _fadeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _fadeTimer.Tick += FadeTimerOnTick;
        _fadeTimer.Start();
    }

    private void FadeTimerOnTick(object? sender, EventArgs e)
    {
        try
        {
            var p = System.Windows.Forms.Control.MousePosition; // ���� ���콺
            double dx = p.X - (Left + Width / 2);
            double dy = p.Y - (Top + Height / 2);
            double dist = Math.Sqrt(dx * dx + dy * dy);
            _targetOpacity = dist < Width * 0.9 ? 1.0 : 0.85; // �ּҰ� 0.85�� ���� (������ ����)
            Opacity = Opacity + (_targetOpacity - Opacity) * 0.25;  // ������ ��¡
        }
        catch { }
    }

    private bool SettingsLocked => _settingsService.Current.Locked; // ��� ����

    private void ToggleVisibility()
    {
        if (Visibility == Visibility.Visible) Hide(); else Show();
    }

    private void SetSize(double s)
    {
        Width = Height = s;
        _settingsService.Current.WindowSize = s;
    }

    private void ToggleLock()
    {
        _settingsService.Current.Locked = !_settingsService.Current.Locked;
        SaveSettings();
    }

    private void SaveSettings()
    {
        _settingsService.Current.WindowLeft = Left;
        _settingsService.Current.WindowTop = Top;
        _settingsService.Save();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (SettingsLocked) return;
        try { DragMove(); } catch { }
    }

    private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var delta = e.Delta > 0 ? 10 : -10;
        var newSize = Math.Clamp(Width + delta, 90, 160);
        SetSize(newSize);
        SaveSettings();
    }

    private void OnClosedInternal()
    {
        _timer.Stop();
        _fadeTimer.Stop();
        _metricsService.Dispose();
        _tray.Dispose();
        SaveSettings();
    }
}
