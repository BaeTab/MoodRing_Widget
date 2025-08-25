// 프로젝트: Mood Ring 위젯
// 파일: TrayService.cs
// 설명: 시스템 트레이(NotifyIcon) 아이콘 생성 및 컨텍스트 메뉴 구성.
//       간단한 링 형태 아이콘을 동적으로 그려 HICON 변환 후 표시.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Mood_Ring.Models;

namespace Mood_Ring.Services;

public class TrayService : IDisposable
{
    private NotifyIcon? _icon;                      // 트레이 아이콘 객체
    private readonly SettingsService _settingsService; // 설정 서비스 (크기 변경 등 반영)
    private readonly Action _toggleVisibility;       // 표시/숨김 토글 액션
    private readonly Action<double> _setSize;        // 크기 조정 액션
    private readonly Action _saveSettings;           // 설정 저장 액션
    private readonly Action _toggleLock;             // 잠금 토글 액션

    private IntPtr _hIcon = IntPtr.Zero;             // 생성된 HICON 핸들 (수동 해제 필요)

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    public TrayService(SettingsService settingsService, Action toggleVisibility, Action<double> setSize, Action saveSettings, Action toggleLock)
    {
        _settingsService = settingsService;
        _toggleVisibility = toggleVisibility;
        _setSize = setSize;
        _saveSettings = saveSettings;
        _toggleLock = toggleLock;
    }

    public void Init()
    {
        var icon = CreateRingIcon();
        _icon = new NotifyIcon
        {
            Text = "Mood Ring",
            Icon = icon,
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
    }

    private Icon CreateRingIcon()
    {
        // 32x32 투명 배경 비트맵에 외곽 링 + 부분 아크를 그려 심볼화
        int size = 32;
        var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            var outerRect = new Rectangle(2, 2, size - 4, size - 4);
            using var outerPen = new Pen(Color.FromArgb(255, 45, 212, 191), 5); // #2dd4bf - 베이스 톤
            g.DrawEllipse(outerPen, outerRect);
            using var innerPen = new Pen(Color.FromArgb(255, 132, 204, 22), 3); // #84cc16 - 진행 느낌
            g.DrawArc(innerPen, outerRect, -90, 220); // 220도 아크 (임의 진행률 표현)
        }
        _hIcon = bmp.GetHicon();
        return Icon.FromHandle(_hIcon);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("표시/숨기기", null, (_,__)=> _toggleVisibility());
        menu.Items.Add("잠금 토글", null, (_,__)=> { _toggleLock(); });
        var sizeMenu = new ToolStripMenuItem("크기");
        foreach (var s in new[]{100,120,140})
        {
            sizeMenu.DropDownItems.Add(s+"px", null, (_,__)=> { _setSize(s); _saveSettings(); });
        }
        menu.Items.Add(sizeMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("종료", null, (_,__)=> { System.Windows.Application.Current.Shutdown(); });
        return menu;
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
