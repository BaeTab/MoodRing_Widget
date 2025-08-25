// 프로젝트: Mood Ring 위젯
// 파일: SystemSnapshot.cs
// 설명: 특정 시점의 시스템 리소스 사용 상태를 담는 불변 DTO.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
using System;

namespace Mood_Ring.Models;

public class SystemSnapshot
{
    public float CpuUsage { get; init; }       // CPU 사용률 % (0~100)
    public float MemoryUsage { get; init; }    // 메모리 사용률 % (실사용/총용량)
    public float DiskBusy { get; init; }       // 디스크 busy time %
    public float NetworkLoad { get; init; }    // 네트워크 상대 부하 스케일 (0~100, 동적 스케일링)
    public float BatteryPercent { get; init; } // 배터리 잔량 % (충전 중에도 값 유지)
    public bool IsCharging { get; init; }      // 전원 연결/충전 여부
}
