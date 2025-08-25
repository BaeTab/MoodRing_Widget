// ������Ʈ: Mood Ring ����
// ����: TrayService.cs
// ����: �ý��� Ʈ����(NotifyIcon) ������ ���� �� ���ؽ�Ʈ �޴� ����.
//       ������ �� ���� �������� �������� �׷� HICON ��ȯ �� ǥ��.
// �Ŀ�: �佺��ũ 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Mood_Ring.Models;

namespace Mood_Ring.Services;

public class TrayService : IDisposable
{
    private NotifyIcon? _icon;                      // Ʈ���� ������ ��ü
    private readonly SettingsService _settingsService; // ���� ���� (ũ�� ���� �� �ݿ�)
    private readonly Action _toggleVisibility;       // ǥ��/���� ��� �׼�
    private readonly Action<double> _setSize;        // ũ�� ���� �׼�
    private readonly Action _saveSettings;           // ���� ���� �׼�
    private readonly Action _toggleLock;             // ��� ��� �׼�

    private IntPtr _hIcon = IntPtr.Zero;             // ������ HICON �ڵ� (���� ���� �ʿ�)

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
        // 32x32 ���� ��� ��Ʈ�ʿ� �ܰ� �� + �κ� ��ũ�� �׷� �ɺ�ȭ
        int size = 32;
        var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            var outerRect = new Rectangle(2, 2, size - 4, size - 4);
            using var outerPen = new Pen(Color.FromArgb(255, 45, 212, 191), 5); // #2dd4bf - ���̽� ��
            g.DrawEllipse(outerPen, outerRect);
            using var innerPen = new Pen(Color.FromArgb(255, 132, 204, 22), 3); // #84cc16 - ���� ����
            g.DrawArc(innerPen, outerRect, -90, 220); // 220�� ��ũ (���� ����� ǥ��)
        }
        _hIcon = bmp.GetHicon();
        return Icon.FromHandle(_hIcon);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("ǥ��/�����", null, (_,__)=> _toggleVisibility());
        menu.Items.Add("��� ���", null, (_,__)=> { _toggleLock(); });
        var sizeMenu = new ToolStripMenuItem("ũ��");
        foreach (var s in new[]{100,120,140})
        {
            sizeMenu.DropDownItems.Add(s+"px", null, (_,__)=> { _setSize(s); _saveSettings(); });
        }
        menu.Items.Add(sizeMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("����", null, (_,__)=> { System.Windows.Application.Current.Shutdown(); });
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
