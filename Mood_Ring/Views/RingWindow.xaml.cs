// 프로젝트: Mood Ring 위젯
// 파일: RingWindow.xaml.cs
// 설명: 프레임리스 투명 TopMost 위젯 창. 드래그 이동, 휠 크기 조절, 더블클릭 모드 순환,
//       호버 상세 패널, 자동 페이드, 클릭 통과, 설정 창 연동, 동적 트레이 아이콘.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Mood_Ring.Helpers;
using Mood_Ring.Services;
using Mood_Ring.ViewModels;

namespace Mood_Ring.Views;

public partial class RingWindow : Window, ITrayHost
{
    private readonly SettingsService _settingsService = new();
    private readonly MetricsService _metricsService = new();
    private readonly MoodService _moodService = new();
    private readonly RingViewModel _vm;
    private readonly TrayService _tray;
    private readonly DispatcherTimer _timer;     // 메트릭 업데이트
    private readonly DispatcherTimer _fadeTimer;  // 자동 페이드 + 팝업
    private SettingsWindow? _settingsWindow;

    public RingWindow()
    {
        InitializeComponent();
        _vm = new RingViewModel(_settingsService, _metricsService, _moodService);
        DataContext = _vm;

        var st = _settingsService.Current;
        Width = Height = st.WindowSize;
        Left = st.WindowLeft;
        Top = st.WindowTop;
        Ring.AnimateColor = st.AnimateColor;

        _tray = new TrayService(this);
        _tray.Init();

        // PerformanceCounter 첫 초기화(수백 ms 소요)를 백그라운드로 워밍업해 시작 시 멈칫 방지
        System.Threading.Tasks.Task.Run(_metricsService.Init);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Max(250, st.UpdateIntervalMs)) };
        _timer.Tick += (_, __) => Tick();
        _timer.Start();

        Loaded += (_, __) => Tick();
        Closed += (_, __) => OnClosedInternal();
        MouseLeftButtonUp += (_, __) => { if (!IsLocked) SaveSettings(); };
        MouseDown += (_, e) => { if (e.ClickCount == 2) { _vm.ToggleDisplayMode(); SaveSettings(); } };

        _fadeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _fadeTimer.Tick += FadeTimerOnTick;
        _fadeTimer.Start();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NativeMethods.MakeToolWindow(this); // Alt+Tab/작업표시줄에서 숨김
        NativeMethods.SetClickThrough(this, _settingsService.Current.ClickThrough);
    }

    private void Tick()
    {
        _vm.Update();
        _tray.UpdateIcon(_vm.RingColor, _vm.CompositeScore / 100.0);
    }

    private void FadeTimerOnTick(object? sender, EventArgs e)
    {
        try
        {
            var st = _settingsService.Current;
            var p = System.Windows.Forms.Control.MousePosition; // 화면 좌표 커서
            double cx = Left + Width / 2, cy = Top + Height / 2;
            double dist = Math.Sqrt((p.X - cx) * (p.X - cx) + (p.Y - cy) * (p.Y - cy));
            bool near = dist < Width * 0.85;

            double idle = Math.Clamp(st.IdleOpacity, 0.3, 1.0);
            double target = near ? 1.0 : idle;
            Opacity += (target - Opacity) * 0.3; // 부드러운 이징

            // 상세 패널: 위젯 또는 팝업 위에 마우스가 있을 때 (플리커 방지)
            bool over = IsMouseOver || (DetailPopup.Child is FrameworkElement fe && fe.IsMouseOver);
            bool showDetail = over && st.ShowDetailOnHover;
            if (DetailPopup.IsOpen != showDetail) DetailPopup.IsOpen = showDetail;
        }
        catch { }
    }

    // ── ITrayHost ────────────────────────────────────────────────────────
    public bool IsLocked => _settingsService.Current.Locked;
    public bool IsClickThrough => _settingsService.Current.ClickThrough;
    public bool IsAutostart => _settingsService.Current.AutoStart;

    public void ToggleVisibility()
    {
        if (Visibility == Visibility.Visible) Hide(); else Show();
    }

    public void SetSizePreset(double size)
    {
        SetSize(size);
        SaveSettings();
    }

    public void OpenSettings()
    {
        if (_settingsWindow != null) { _settingsWindow.Activate(); return; }
        _settingsWindow = new SettingsWindow(_settingsService, ApplySettings);
        _settingsWindow.Closed += (_, __) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    public void ToggleLock()
    {
        _settingsService.Current.Locked = !_settingsService.Current.Locked;
        SaveSettings();
    }

    public void ToggleClickThrough()
    {
        bool v = !_settingsService.Current.ClickThrough;
        _settingsService.Current.ClickThrough = v;
        NativeMethods.SetClickThrough(this, v);
        SaveSettings();
    }

    public void ToggleAutostart()
    {
        bool v = !_settingsService.Current.AutoStart;
        if (AutostartService.SetEnabled(v))
        {
            _settingsService.Current.AutoStart = v;
            SaveSettings();
        }
    }

    // 설정 창에서 값이 바뀌면 호출 — 즉시 반영
    public void ApplySettings()
    {
        var st = _settingsService.Current;
        _timer.Interval = TimeSpan.FromMilliseconds(Math.Max(250, st.UpdateIntervalMs));
        Ring.AnimateColor = st.AnimateColor;
        NativeMethods.SetClickThrough(this, st.ClickThrough);
        if (!AutostartService.SetEnabled(st.AutoStart))
            st.AutoStart = !st.AutoStart; // 레지스트리 실패 시 모델을 되돌려 JSON/레지스트리 불일치 방지
        if (Math.Abs(Width - st.WindowSize) > 0.5) Width = Height = st.WindowSize;
        _settingsService.Save();
        Tick(); // 테마/글로우 즉시 반영
    }

    // ── 내부 동작 ────────────────────────────────────────────────────────
    private void SetSize(double s)
    {
        Width = Height = s;
        _settingsService.Current.WindowSize = s;
    }

    private void SaveSettings()
    {
        var st = _settingsService.Current;
        st.WindowLeft = Left;
        st.WindowTop = Top;
        _settingsService.Save();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsLocked) return;
        try { DragMove(); } catch { }
    }

    private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (IsLocked) return;
        var delta = e.Delta > 0 ? 10 : -10;
        SetSize(Math.Clamp(Width + delta, 90, 220));
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
