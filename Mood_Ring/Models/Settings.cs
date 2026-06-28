// 프로젝트: Mood Ring 위젯
// 파일: Settings.cs
// 설명: 사용자 설정(가중치, 업데이트 주기, 창 크기/위치, 표시 모드, 비주얼 옵션 등)을 표현.
// 후원: 토스뱅크 1001-2269-0600 (개발 지속에 큰 힘이 됩니다!)
// ----------------------------------------------------------------------------------
namespace Mood_Ring.Models;

public class Settings
{
    // ── 가중치 (합이 1이 되도록 기본값 구성) ──────────────────────────────
    public double CpuWeight { get; set; } = 0.40;     // CPU 점수 반영 비율
    public double MemoryWeight { get; set; } = 0.30;  // 메모리 점수 반영 비율
    public double DiskWeight { get; set; } = 0.15;    // 디스크 Busy 비율
    public double NetworkWeight { get; set; } = 0.15; // 네트워크 사용량 비율

    public bool BatteryAdjustment { get; set; } = true; // 배터리 상태 기반 가감점 on/off
    public int UpdateIntervalMs { get; set; } = 1000;   // 시스템 메트릭 샘플링 주기 (ms)
    public double Alpha { get; set; } = 0.2;            // EMA 계수(0~1) — 작을수록 더 부드럽게

    // ── 창 크기/위치 ─────────────────────────────────────────────────────
    public double WindowSize { get; set; } = 140; // 링 크기(px)
    public double WindowLeft { get; set; } = 100; // 좌상단 X
    public double WindowTop { get; set; } = 100;  // 좌상단 Y

    // 표시 모드: 수치(Score) / 이모지(Emoji) / 텍스트(Text)
    public string DisplayMode { get; set; } = "Score";
    public bool Locked { get; set; } = false;     // 잠금 시 드래그/크기 변경 비활성화
    public bool AutoStart { get; set; } = false;  // 윈도우 시작 시 자동 실행 (레지스트리 Run)

    // ── 고도화 비주얼/동작 옵션 ──────────────────────────────────────────
    public string ColorTheme { get; set; } = "Aurora"; // Aurora/Sunset/Neon/Ocean/Mono
    public bool ReactiveGlow { get; set; } = true;      // 점수 기반 글로우/펄스 강도·속도
    public bool AnimateColor { get; set; } = true;      // 색 전환 애니메이션(eased)
    public bool ClickThrough { get; set; } = false;     // 클릭 통과(위젯 위 클릭 무시)
    public double IdleOpacity { get; set; } = 0.85;     // 마우스가 멀 때 최소 투명도
    public bool ShowDetailOnHover { get; set; } = true; // 호버 시 상세 패널 표시
    public bool ShowSparkline { get; set; } = true;     // 상세 패널에 스파크라인 표시
}
