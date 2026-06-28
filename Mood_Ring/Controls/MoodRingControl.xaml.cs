// 프로젝트: Mood Ring 위젯
// 파일: MoodRingControl.xaml.cs
// 설명: 무드 링 표시 UserControl. Progress(0~100)로 아크를 그리고,
//       RingColor/GlowIntensity/MoodBand에 따라 색 전환·글로우 호흡·과열 샤이머를 연출.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Color = System.Windows.Media.Color;   // WinForms와 모호성 제거
using Point = System.Windows.Point;

namespace Mood_Ring.Controls;

public partial class MoodRingControl : System.Windows.Controls.UserControl
{
    private readonly SolidColorBrush _ringBrush = new(Colors.Teal);   // 진행 아크/글로우 공유 (애니메이션 대상)
    private readonly SolidColorBrush _cometBrush = new(Colors.White); // 코멧(아크 끝 점)
    private Storyboard? _pulse; // 글로우 호흡
    private Storyboard? _spin;  // 샤이머 회전

    // ── DependencyProperty 정의 ──────────────────────────────────────────
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(MoodRingControl),
        new PropertyMetadata(0.0, (d, e) => ((MoodRingControl)d).UpdateArc((double)e.NewValue)));
    public double Progress { get => (double)GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }

    public static readonly DependencyProperty RingColorProperty = DependencyProperty.Register(
        nameof(RingColor), typeof(Color), typeof(MoodRingControl),
        new PropertyMetadata(Colors.Teal, (d, e) => ((MoodRingControl)d).ApplyColor((Color)e.NewValue)));
    public Color RingColor { get => (Color)GetValue(RingColorProperty); set => SetValue(RingColorProperty, value); }

    public static readonly DependencyProperty GlowIntensityProperty = DependencyProperty.Register(
        nameof(GlowIntensity), typeof(double), typeof(MoodRingControl),
        new PropertyMetadata(0.42, (d, e) => ((MoodRingControl)d).ApplyGlow((double)e.NewValue)));
    public double GlowIntensity { get => (double)GetValue(GlowIntensityProperty); set => SetValue(GlowIntensityProperty, value); }

    public static readonly DependencyProperty MoodBandProperty = DependencyProperty.Register(
        nameof(MoodBand), typeof(int), typeof(MoodRingControl),
        new PropertyMetadata(0, (d, e) => ((MoodRingControl)d).ApplyBand((int)e.NewValue)));
    public int MoodBand { get => (int)GetValue(MoodBandProperty); set => SetValue(MoodBandProperty, value); }

    public static readonly DependencyProperty AnimateColorProperty = DependencyProperty.Register(
        nameof(AnimateColor), typeof(bool), typeof(MoodRingControl), new PropertyMetadata(true));
    public bool AnimateColor { get => (bool)GetValue(AnimateColorProperty); set => SetValue(AnimateColorProperty, value); }

    public MoodRingControl()
    {
        InitializeComponent();
        ProgressPath.Stroke = _ringBrush;
        Glow.Stroke = _ringBrush;
        Comet.Fill = _cometBrush;
        Loaded += (_, __) =>
        {
            Relayout();
            StartPulse();
            ApplyColor(RingColor);
            ApplyGlow(GlowIntensity);
            ApplyBand(MoodBand);
        };
        SizeChanged += (_, __) => Relayout();
    }

    private double Size => Math.Min(
        (double.IsNaN(ActualWidth) || ActualWidth <= 0) ? Width : ActualWidth,
        (double.IsNaN(ActualHeight) || ActualHeight <= 0) ? Height : ActualHeight);

    private void Relayout()
    {
        BuildTicks();
        UpdateArc(Progress);
    }

    // 60개 눈금 (5의 배수는 길게) — 트랙 안쪽에 은은하게
    private void BuildTicks()
    {
        double size = Size;
        if (size <= 0 || double.IsNaN(size) || Ticks == null) return;
        double thickness = 9;
        double ringR = (size - thickness) / 2.0;
        double tickOuter = ringR - thickness / 2.0 - 2;
        if (tickOuter <= 4) { Ticks.Data = null; return; }
        double cx = size / 2.0, cy = size / 2.0;
        var geo = new GeometryGroup();
        for (int i = 0; i < 60; i++)
        {
            bool major = i % 5 == 0;
            double len = major ? 6 : 3;
            double ri = Math.Max(2, tickOuter - len);
            double a = i / 60.0 * 2 * Math.PI;
            var p1 = new Point(cx + tickOuter * Math.Sin(a), cy - tickOuter * Math.Cos(a));
            var p2 = new Point(cx + ri * Math.Sin(a), cy - ri * Math.Cos(a));
            geo.Children.Add(new LineGeometry(p1, p2));
        }
        geo.Freeze();
        Ticks.Data = geo;
    }

    // 진행 아크 (12시 시작, 시계방향)
    private void UpdateArc(double value)
    {
        if (ProgressPath == null) return;
        double size = Size;
        if (size <= 0 || double.IsNaN(size)) return;
        double thickness = ProgressPath.StrokeThickness;
        double r = (size - thickness) / 2.0;
        if (r <= 0) return;
        double cx = size / 2.0, cy = size / 2.0;
        value = Math.Clamp(value, 0, 100);
        double angle = value / 100.0 * 360.0;

        if (angle < 0.01)
        {
            var dot = Geometry.Parse($"M{F(cx)},{F(cy - r)}");
            dot.Freeze();
            ProgressPath.Data = dot;
            SheenPath.Data = dot;
            Comet.Visibility = Visibility.Collapsed;
            return;
        }

        Geometry g;
        if (angle > 359.9)
        {
            g = new EllipseGeometry(new Point(cx, cy), r, r);
        }
        else
        {
            double rad = Math.PI / 180.0 * angle;
            double ex = cx + r * Math.Sin(rad);
            double ey = cy - r * Math.Cos(rad);
            bool large = angle > 180;
            g = Geometry.Parse($"M{F(cx)},{F(cy - r)} A{F(r)},{F(r)} 0 {(large ? 1 : 0)} 1 {F(ex)},{F(ey)}");
        }
        g.Freeze();
        ProgressPath.Data = g;
        SheenPath.Data = g;

        // 코멧을 아크 끝에 배치
        double rad2 = Math.PI / 180.0 * angle;
        double tx = cx + r * Math.Sin(rad2);
        double ty = cy - r * Math.Cos(rad2);
        Comet.Visibility = Visibility.Visible;
        Comet.Margin = new Thickness(tx - Comet.Width / 2, ty - Comet.Height / 2, 0, 0);
    }

    // 색 전환 (옵션에 따라 eased 애니메이션 / 즉시 적용)
    private void ApplyColor(Color c)
    {
        var bright = Lighten(c, 0.4);
        if (AnimateColor && IsLoaded)
        {
            var dur = new Duration(TimeSpan.FromMilliseconds(360));
            _ringBrush.BeginAnimation(SolidColorBrush.ColorProperty,
                new ColorAnimation(c, dur) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
            _cometBrush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(bright, dur));
        }
        else
        {
            _ringBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
            _ringBrush.Color = c;
            _cometBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
            _cometBrush.Color = bright;
        }
    }

    // 글로우 강도 (점수 기반) — 불투명도 + 번짐 반경
    private void ApplyGlow(double intensity)
    {
        intensity = Math.Clamp(intensity, 0.15, 0.95);
        if (IsLoaded)
            Glow.BeginAnimation(OpacityProperty, new DoubleAnimation(intensity, new Duration(TimeSpan.FromMilliseconds(500))));
        else
            Glow.Opacity = intensity;
        if (GlowBlur != null) GlowBlur.Radius = 12 + intensity * 16;
    }

    // 밴드(0~3): 펄스 속도 + 샤이머 노출/회전
    private void ApplyBand(int band)
    {
        band = Math.Clamp(band, 0, 3);
        double ratio = band switch { 0 => 0.6, 1 => 0.9, 2 => 1.3, _ => 1.9 };
        try { _pulse?.SetSpeedRatio(this, ratio); } catch { }

        double targetOpacity = band >= 3 ? 0.55 : (band == 2 ? 0.18 : 0.0);
        if (IsLoaded)
            Shimmer.BeginAnimation(OpacityProperty, new DoubleAnimation(targetOpacity, new Duration(TimeSpan.FromMilliseconds(500))));
        else
            Shimmer.Opacity = targetOpacity;

        if (band >= 2) StartSpin(band);
        else StopSpin();
    }

    private void StartPulse()
    {
        if (_pulse != null) return;
        var dur = new Duration(TimeSpan.FromSeconds(1.7));
        var ease = new SineEase { EasingMode = EasingMode.EaseInOut };
        var ax = new DoubleAnimation(1.0, 1.06, dur) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = ease };
        var ay = ax.Clone();
        Storyboard.SetTargetName(ax, "GlowScale");
        Storyboard.SetTargetProperty(ax, new PropertyPath(ScaleTransform.ScaleXProperty));
        Storyboard.SetTargetName(ay, "GlowScale");
        Storyboard.SetTargetProperty(ay, new PropertyPath(ScaleTransform.ScaleYProperty));
        _pulse = new Storyboard();
        _pulse.Children.Add(ax);
        _pulse.Children.Add(ay);
        try { _pulse.Begin(this, true); } catch { _pulse = null; }
    }

    private void StartSpin(int band)
    {
        if (_spin == null)
        {
            var a = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(7.0))) { RepeatBehavior = RepeatBehavior.Forever };
            Storyboard.SetTargetName(a, "ShimmerRot");
            Storyboard.SetTargetProperty(a, new PropertyPath(RotateTransform.AngleProperty));
            _spin = new Storyboard();
            _spin.Children.Add(a);
            try { _spin.Begin(this, true); } catch { _spin = null; return; }
        }
        try { _spin.SetSpeedRatio(this, band >= 3 ? 2.0 : 1.0); } catch { }
    }

    private void StopSpin()
    {
        if (_spin == null) return;
        try { _spin.Stop(this); } catch { }
        _spin = null; // 보이지 않을 때 회전 클럭 해제 (애니메이션 스레드 부담 제거)
    }

    private static Color Lighten(Color c, double amt)
    {
        byte L(byte v) => (byte)(v + (255 - v) * amt);
        return Color.FromRgb(L(c.R), L(c.G), L(c.B));
    }

    private static string F(double d) => d.ToString(CultureInfo.InvariantCulture);
}
