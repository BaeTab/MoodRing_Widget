// 프로젝트: Mood Ring 위젯
// 파일: RingWindow.xaml.cs
// 설명: 프레임리스 투명 TopMost 위젯 창. 드래그 이동, 크기 조절(휠), 더블클릭 모드 토글,
//       자동 페이드(마우스 거리 기반), 트레이 서비스 초기화 및 주기적 업데이트 관리.
// 후원: 토스뱅크 1001-2269-0600
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
    private readonly SettingsService _settingsService = new(); // 사용자 설정
    private readonly MetricsService _metricsService = new();     // 메트릭 수집
    private readonly MoodService _moodService = new();           // 점수/색 계산
    private readonly RingViewModel _vm;                          // ViewModel 인스턴스
    private readonly TrayService _tray;                          // 트레이 아이콘
    private readonly DispatcherTimer _timer;                     // 메트릭 업데이트 타이머
    private readonly DispatcherTimer _fadeTimer;                 // 자동 투명도 타이머

    private double _targetOpacity = 1.0;                         // 페이드 목표값 (가독성 강화 위해 최소 0.85)

    public RingWindow()
    {
        InitializeComponent();
        _vm = new RingViewModel(_settingsService, _metricsService, _moodService);
        DataContext = _vm;

        // 위치/크기 복원
        Width = Height = _settingsService.Current.WindowSize;
        Left = _settingsService.Current.WindowLeft;
        Top = _settingsService.Current.WindowTop;

        // 트레이 초기화
        _tray = new TrayService(_settingsService, ToggleVisibility, SetSize, SaveSettings, ToggleLock);
        _tray.Init();

        // 주기 업데이트 타이머 (CompositeScore/색상)
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_settingsService.Current.UpdateIntervalMs) };
        _timer.Tick += (_, __) => _vm.Update();
        _timer.Start();
        Loaded += (_, __) => _vm.Update(); // 최초 즉시 한 번
        Closed += (_, __) => OnClosedInternal();
        MouseLeftButtonUp += (_, __) => { if (!SettingsLocked) { SaveSettings(); } }; // 드래그 종료 시 위치 저장
        MouseDown += (_, e) => { if (e.ClickCount == 2) { _vm.ToggleDisplayMode(); SaveSettings(); } }; // 더블클릭 표시 모드 순환

        // 자동 페이드 (마우스 근접=1.0 / 원거리=0.85) - 500ms 주기 점진 보간
        _fadeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _fadeTimer.Tick += FadeTimerOnTick;
        _fadeTimer.Start();
    }

    private void FadeTimerOnTick(object? sender, EventArgs e)
    {
        try
        {
            var p = System.Windows.Forms.Control.MousePosition; // 전역 마우스
            double dx = p.X - (Left + Width / 2);
            double dy = p.Y - (Top + Height / 2);
            double dist = Math.Sqrt(dx * dx + dy * dy);
            _targetOpacity = dist < Width * 0.9 ? 1.0 : 0.85; // 최소값 0.85로 상향 (가독성 개선)
            Opacity = Opacity + (_targetOpacity - Opacity) * 0.25;  // 간단한 이징
        }
        catch { }
    }

    private bool SettingsLocked => _settingsService.Current.Locked; // 잠금 여부

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
