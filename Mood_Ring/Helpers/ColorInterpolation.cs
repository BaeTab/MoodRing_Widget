// 프로젝트: Mood Ring 위젯
// 파일: ColorInterpolation.cs
// 설명: HSV 공간에서 색상 보간(선형) 유틸리티. 링 색상 전환을 부드럽게 처리.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;
using MediaColor = System.Windows.Media.Color; // System.Drawing.Color와 혼동 방지 별칭
using System.Windows.Media;

namespace Mood_Ring.Helpers;

public static class ColorInterpolation
{
    // HSV 보간 유틸 (H[0-360), S,V[0-1])
    public static MediaColor LerpHsv(MediaColor a, MediaColor b, double t)
    {
        (double h1, double s1, double v1) = RgbToHsv(a);
        (double h2, double s2, double v2) = RgbToHsv(b);
        // 가장 짧은 hue 차를 따라 이동 (색상 단절 최소화)
        double dh = h2 - h1;
        if (dh > 180) dh -= 360; else if (dh < -180) dh += 360;
        double h = (h1 + t * dh + 360) % 360;
        double s = s1 + (s2 - s1) * t;
        double v = v1 + (v2 - v1) * t;
        return HsvToRgb(h, s, v);
    }

    // RGB -> HSV 변환 (UI 색상 보간 효율을 위해)
    public static (double h, double s, double v) RgbToHsv(MediaColor c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double h, s, v = max;
        double d = max - min;
        s = max == 0 ? 0 : d / max; // 채도
        if (d == 0) h = 0; // 무채색
        else
        {
            if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;
            h *= 60; // 섹터 -> 도(degree)
        }
        return (h, s, v);
    }

    // HSV -> RGB 역변환
    public static MediaColor HsvToRgb(double h, double s, double v)
    {
        int i = (int)Math.Floor(h / 60) % 6;
        double f = h / 60 - Math.Floor(h / 60);
        double p = v * (1 - s);
        double q = v * (1 - f * s);
        double t = v * (1 - (1 - f) * s);
        double r=0,g=0,b=0;
        switch (i)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            case 5: r = v; g = p; b = q; break;
        }
        return MediaColor.FromRgb((byte)(r*255),(byte)(g*255),(byte)(b*255));
    }
}
