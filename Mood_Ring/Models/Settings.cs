// 프로젝트: Mood Ring 위젯
// 파일: Settings.cs
// 설명: 사용자 설정(가중치, 업데이트 간격, 창 크기/위치, 표시 모드 등)을 표현.
// 후원: 토스뱅크 1001-2269-0600 (개발 지원 감사합니다!)
// ----------------------------------------------------------------------------------
using System.Text.Json.Serialization;

namespace Mood_Ring.Models;

public class Settings
{
    // 가중치 (합이 1이 되도록 기본값 지정) - CPU 40%, 메모리 30%, 디스크 15%, 네트워크 15%
    // 필요 시 사용자 설정에서 조정 가능 (TODO: UI 설정 패널)
    public double CpuWeight { get; set; } = 0.40;   // CPU 사용률 반영 비중
    public double MemoryWeight { get; set; } = 0.30; // 메모리 사용률 반영 비중
    public double DiskWeight { get; set; } = 0.15;   // 디스크 Busy 비중
    public double NetworkWeight { get; set; } = 0.15; // 네트워크 상대 부하 비중

    public bool BatteryAdjustment { get; set; } = true; // 배터리 상태에 따른 가산/감산 보정 on/off
    public int UpdateIntervalMs { get; set; } = 1000;   // 시스템 메트릭 샘플링 주기 (ms)

    public double Alpha { get; set; } = 0.2; // EMA(지수 이동 평균) 계수 (0~1) - 낮을수록 더 부드러운 변화

    // 창 크기/위치 (다중 모니터 고려 간단 보존 - 추후 화면 밖 보정 로직 추가 가능)
    public double WindowSize { get; set; } = 120; // 링 크기(px)
    public double WindowLeft { get; set; } = 100; // 저장된 X 위치
    public double WindowTop { get; set; } = 100;  // 저장된 Y 위치

    // 표시 모드: 수치(Score) / 이모지(Emoji) / 텍스트(Text)
    public string DisplayMode { get; set; } = "Score"; 
    public bool Locked { get; set; } = false; // 잠금 시 드래그/크기 변경 비활성화

    public bool AutoStart { get; set; } = false; // (TODO) 시작 프로그램 등록 여부
}
