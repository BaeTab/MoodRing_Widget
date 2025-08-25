// ������Ʈ: Mood Ring ����
// ����: MoodRingControl.xaml.cs
// ����: �� ǥ�� UserControl. Progress(0~100)�� ���� Path ��ũ ����.
//       ���� �߰� ȿ��(�β�/Glow ��ȭ, ���� �ִϸ��̼�) Ȯ�� ����.
// �Ŀ�: �佺��ũ 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;
using System.Windows;
using System.Windows.Media;

namespace Mood_Ring.Controls;

public partial class MoodRingControl : System.Windows.Controls.UserControl
{
    // ���� �Ӽ�: ����� (CompositeScore ���ε� ���)
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
        Loaded += (_, __) => UpdateArc(Progress); // ù ���� �� ��ũ ���
        SizeChanged += (_, __) => UpdateArc(Progress); // �������� ����
    }

    private void UpdateArc(double value)
    {
        if (ProgressPath == null) return; // XAML ��� ���� ���� �� �� ���
        double w = ActualWidth; if (double.IsNaN(w) || w <= 0) w = Width; if (w <= 0) return;
        double h = ActualHeight; if (double.IsNaN(h) || h <= 0) h = Height; if (h <= 0) return;
        double size = Math.Min(w, h);
        double thickness = ProgressPath.StrokeThickness; // StrokeThickness ��� ������ ����
        double r = (size - thickness) / 2.0;              // ���� ��� ���
        double cx = size / 2.0;
        double cy = size / 2.0;
        double angle = value / 100.0 * 360.0;             // ���� �� ����(��)

        if (angle < 0.01)
        {
            ProgressPath.Data = Geometry.Parse($"M{cx},{cy - r}"); // �������� (0%)
            return;
        }
        if (angle > 359.9)
        {
            // 100%: ��ü �� (EllipseGeometry ��� �� ���� ����ȭ)
            ProgressPath.Data = new EllipseGeometry(new System.Windows.Point(cx, cy), r, r);
            return;
        }

        // ���� ��ǥ (12�� ���� �ð����)
        double rad = (Math.PI / 180.0) * angle;
        double ex = cx + r * Math.Sin(rad);
        double ey = cy - r * Math.Cos(rad);
        bool largeArc = angle > 180; // 180�� �ʰ� ����

        string data = $"M{cx},{cy - r} A{r},{r} 0 {(largeArc ? 1 : 0)} 1 {ex},{ey}"; // ���: Move + Arc
        try { ProgressPath.Data = Geometry.Parse(data); } catch { /* �Ľ� ���н� ���� */ }
    }
}
