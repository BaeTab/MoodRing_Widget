// ������Ʈ: Mood Ring ����
// ����: MoodService.cs
// ����: �ý��� ������ �� ���� ����(CompositeScore) ���, EMA ������, ��������� ����.
//       ���͸� ���¿� ���� ���� ���� ����. ���� �������� ���̱� ���� Raw(���) ������ ����.
// �Ŀ�: �佺��ũ 1001-2269-0600
// ----------------------------------------------------------------------------------
using Mood_Ring.Helpers;
using Mood_Ring.Models;
using System.Windows.Media;

namespace Mood_Ring.Services;

public class MoodService
{
    private double? _ema;                 // EMA ������ (null�̸� �ʱ�ȭ �ʿ�)
    public double LastRawScore { get; private set; } // �ֱ� ���� ���(raw) ���� (����+Ŭ���� ��, EMA ���� ��)

    // ���� ������ ���� ���� (HSV ���� ���)
    private readonly (double pos, System.Windows.Media.Color color)[] _stops = new[]
    {
        (0.0,  (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2dd4bf")!),
        (25.0, (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2dd4bf")!),
        (50.0, (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#84cc16")!),
        (75.0, (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#fb923c")!),
        (100.0,(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ef4444")!)
    };

    public double ComputeComposite(SystemSnapshot s, Models.Settings settings)
    {
        // ������ (�� ��Ʈ�� 0~100)
        double raw = s.CpuUsage * settings.CpuWeight +
                     s.MemoryUsage * settings.MemoryWeight +
                     s.DiskBusy * settings.DiskWeight +
                     s.NetworkLoad * settings.NetworkWeight;

        // ���͸� ����
        if (settings.BatteryAdjustment)
        {
            if (s.IsCharging) raw -= 5;
            else if (s.BatteryPercent < 20) raw += 10;
        }

        // Ŭ���� �� Raw ���� ���� (���� ��� ������ ���)
        raw = raw < 0 ? 0 : (raw > 100 ? 100 : raw);
        LastRawScore = raw; // ������ EMA ��� �� ���� ����Ͽ� �ΰ��ϰ� ����

        // EMA �������� ��ġ ǥ�� ����ȭ (�ð��� ���� ���� ��鸲 ����)
        _ema = _ema is null ? raw : settings.Alpha * raw + (1 - settings.Alpha) * _ema.Value;
        return _ema.Value; // ��ȯ���� "������ ����" (CompositeScore)
    }

    // value(0~100)�� ���� HSV ���� �귯�� (��� ���� ���� ����)
    public SolidColorBrush GetBrush(double value)
    {
        if (value <= _stops[0].pos) return Frozen(_stops[0].color);
        for (int i = 1; i < _stops.Length; i++)
        {
            if (value <= _stops[i].pos)
            {
                var (p1, c1) = _stops[i - 1];
                var (p2, c2) = _stops[i];
                double t = (value - p1) / (p2 - p1);
                var c = ColorInterpolation.LerpHsv(c1, c2, t);
                return Frozen(c);
            }
        }
        return Frozen(_stops[^1].color);
    }

    private SolidColorBrush Frozen(System.Windows.Media.Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze(); // Freezable �� ���� ����ȭ
        return b;
    }
}
