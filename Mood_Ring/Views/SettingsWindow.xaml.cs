// 프로젝트: Mood Ring 위젯
// 파일: SettingsWindow.xaml.cs
// 설명: 설정 패널. Settings 모델에 직접 바인딩하고 변경 시 즉시 apply 콜백으로 위젯에 반영.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;
using System.Windows;
using System.Windows.Input;
using Mood_Ring.Models;
using Mood_Ring.Services;

namespace Mood_Ring.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settings;
    private readonly Action _apply; // 변경 즉시 위젯 반영 + 저장
    private bool _ready;

    public SettingsWindow(SettingsService settings, Action apply)
    {
        _settings = settings;
        _apply = apply;
        InitializeComponent();
        ThemeCombo.ItemsSource = MoodService.ThemeNames;
        ModeCombo.ItemsSource = new[] { "Score", "Emoji", "Text" };
        DataContext = _settings.Current;
        _ready = true;
    }

    private void Commit()
    {
        if (!_ready) return;
        try { _apply(); } catch { /* 설정 적용 실패가 설정 창을 죽이지 않도록 */ }
    }

    private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => Commit();
    private void OnToggle(object sender, RoutedEventArgs e) => Commit();
    private void OnComboChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) => Commit();

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        var d = new Settings();
        var c = _settings.Current;
        c.CpuWeight = d.CpuWeight; c.MemoryWeight = d.MemoryWeight; c.DiskWeight = d.DiskWeight; c.NetworkWeight = d.NetworkWeight;
        c.Alpha = d.Alpha; c.UpdateIntervalMs = d.UpdateIntervalMs; c.IdleOpacity = d.IdleOpacity;
        c.BatteryAdjustment = d.BatteryAdjustment; c.ReactiveGlow = d.ReactiveGlow; c.AnimateColor = d.AnimateColor;
        c.ClickThrough = d.ClickThrough; c.ShowDetailOnHover = d.ShowDetailOnHover;
        c.ColorTheme = d.ColorTheme; c.DisplayMode = d.DisplayMode;

        // Settings는 INotifyPropertyChanged가 아니므로 강제 리바인딩으로 UI 갱신
        _ready = false;
        DataContext = null;
        DataContext = c;
        _ready = true;
        Commit();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        try { DragMove(); } catch { }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
