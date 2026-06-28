// 프로젝트: Mood Ring 위젯
// 파일: TrayService.cs
// 설명: 시스템 트레이(NotifyIcon) 아이콘 및 컨텍스트 메뉴. 점수 색을 반영한 동적 아이콘 갱신.
//       메뉴 동작은 ITrayHost(RingWindow)에 위임.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MediaColor = System.Windows.Media.Color;

namespace Mood_Ring.Services;

// 트레이 메뉴가 호출하는 동작/상태 (RingWindow가 구현)
public interface ITrayHost
{
    void ToggleVisibility();
    void SetSizePreset(double size);
    void OpenSettings();
    void ToggleLock();
    void ToggleClickThrough();
    void ToggleAutostart();
    bool IsLocked { get; }
    bool IsClickThrough { get; }
    bool IsAutostart { get; }
}

public class TrayService : IDisposable
{
    private NotifyIcon? _icon;
    private readonly ITrayHost _host;
    private IntPtr _hIcon = IntPtr.Zero; // 현재 아이콘 HICON (수동 해제 필요)
    private int _lastColorKey = -1;       // 색 변화 감지(불필요한 재생성 방지)

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public TrayService(ITrayHost host) => _host = host;

    public void Init()
    {
        _icon = new NotifyIcon
        {
            Text = "Mood Ring",
            Icon = BuildIcon(MediaColor.FromRgb(45, 212, 191), 0.4),
            Visible = true
        };
        var menu = new ContextMenuStrip();
        menu.Opening += (_, __) => RebuildMenu(menu); // 열 때마다 체크 상태 갱신
        _icon.ContextMenuStrip = menu;
        _icon.DoubleClick += (_, __) => _host.ToggleVisibility();
    }

    private void RebuildMenu(ContextMenuStrip menu)
    {
        menu.Items.Clear();
        menu.Items.Add("표시 / 숨기기", null, (_, __) => _host.ToggleVisibility());
        menu.Items.Add("설정…", null, (_, __) => _host.OpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("잠금", null, (_, __) => _host.ToggleLock()) { Checked = _host.IsLocked });
        menu.Items.Add(new ToolStripMenuItem("클릭 통과", null, (_, __) => _host.ToggleClickThrough()) { Checked = _host.IsClickThrough });
        menu.Items.Add(new ToolStripMenuItem("윈도우 시작 시 실행", null, (_, __) => _host.ToggleAutostart()) { Checked = _host.IsAutostart });

        var sizeMenu = new ToolStripMenuItem("크기");
        foreach (var s in new[] { 100, 120, 140, 160 })
            sizeMenu.DropDownItems.Add(s + "px", null, (_, __) => _host.SetSizePreset(s));
        menu.Items.Add(sizeMenu);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("종료", null, (_, __) => System.Windows.Application.Current.Shutdown());
    }

    // 점수 색으로 트레이 아이콘 갱신 (작은 링 + 진행 아크)
    public void UpdateIcon(MediaColor c, double fill)
    {
        if (_icon == null) return;
        int key = ((c.R >> 5) << 10) | ((c.G >> 5) << 5) | (c.B >> 5);
        if (key == _lastColorKey) return; // 색 변화 미미하면 스킵
        _lastColorKey = key;

        var old = _hIcon;
        _icon.Icon = BuildIcon(c, fill); // 내부에서 _hIcon 갱신
        if (old != IntPtr.Zero) DestroyIcon(old);
    }

    private Icon BuildIcon(MediaColor c, double fill)
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            var rect = new Rectangle(4, 4, size - 8, size - 8);
            using (var track = new Pen(Color.FromArgb(90, 120, 130, 150), 5))
                g.DrawEllipse(track, rect);
            using var pen = new Pen(Color.FromArgb(255, c.R, c.G, c.B), 5)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            float sweep = (float)(Math.Clamp(fill, 0, 1) * 360.0);
            if (sweep > 0.5f) g.DrawArc(pen, rect, -90, sweep);
        }
        _hIcon = bmp.GetHicon();
        return Icon.FromHandle(_hIcon);
    }

    public void Dispose()
    {
        if (_icon != null)
        {
            _icon.Visible = false;
            _icon.Dispose();
        }
        if (_hIcon != IntPtr.Zero)
        {
            DestroyIcon(_hIcon);
            _hIcon = IntPtr.Zero;
        }
    }
}
