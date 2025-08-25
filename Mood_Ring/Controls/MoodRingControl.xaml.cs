// 프로젝트: Mood Ring 위젯
// 파일: MoodRingControl.xaml.cs
// 설명: 링 표현 UserControl. Progress(0~100)에 따라 Path 아크 갱신.
//       향후 추가 효과(두께/Glow 변화, 색상 애니메이션) 확장 지점.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;
using System.Windows;
using System.Windows.Media;

namespace Mood_Ring.Controls;

public partial class MoodRingControl : System.Windows.Controls.UserControl
{
    // 의존 속성: 진행률 (CompositeScore 바인딩 대상)
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(MoodRingControl),
        new PropertyMetadata(0.0, OnProgressChanged));

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MoodRingControl c) c.UpdateArc((double)e.NewValue);
    }

    public MoodRingControl()
    {
        InitializeComponent();
        Loaded += (_, __) => UpdateArc(Progress); // 첫 렌더 시 아크 계산
        SizeChanged += (_, __) => UpdateArc(Progress); // 리사이즈 대응
    }

    private void UpdateArc(double value)
    {
        if (ProgressPath == null) return; // XAML 요소 아직 생성 안 된 경우
        double w = ActualWidth; if (double.IsNaN(w) || w <= 0) w = Width; if (w <= 0) return;
        double h = ActualHeight; if (double.IsNaN(h) || h <= 0) h = Height; if (h <= 0) return;
        double size = Math.Min(w, h);
        double thickness = ProgressPath.StrokeThickness; // StrokeThickness 기반 반지름 산정
        double r = (size - thickness) / 2.0;              // 내부 경계 고려
        double cx = size / 2.0;
        double cy = size / 2.0;
        double angle = value / 100.0 * 360.0;             // 점수 → 각도(도)

        if (angle < 0.01)
        {
            ProgressPath.Data = Geometry.Parse($"M{cx},{cy - r}"); // 시작점만 (0%)
            return;
        }
        if (angle > 359.9)
        {
            // 100%: 전체 원 (EllipseGeometry 사용 → 렌더 최적화)
            ProgressPath.Data = new EllipseGeometry(new System.Windows.Point(cx, cy), r, r);
            return;
        }

        // 끝점 좌표 (12시 기준 시계방향)
        double rad = (Math.PI / 180.0) * angle;
        double ex = cx + r * Math.Sin(rad);
        double ey = cy - r * Math.Cos(rad);
        bool largeArc = angle > 180; // 180도 초과 여부

        string data = $"M{cx},{cy - r} A{r},{r} 0 {(largeArc ? 1 : 0)} 1 {ex},{ey}"; // 명령: Move + Arc
        try { ProgressPath.Data = Geometry.Parse(data); } catch { /* 파싱 실패시 무시 */ }
    }
}
