// 프로젝트: Mood Ring 위젯
// 파일: SystemSnapshot.cs
// 설명: 특정 시점의 시스템 리소스 사용 상태를 담는 불변 DTO.
// 후원: 토스뱅크 1001-2269-0600
// ----------------------------------------------------------------------------------
namespace Mood_Ring.Models;

public class SystemSnapshot
{
    public float CpuUsage { get; init; }       // CPU 사용 % (0~100)
    public float MemoryUsage { get; init; }    // 메모리 사용 % (사용량/총용량)
    public float DiskBusy { get; init; }       // 디스크 busy time %
    public float NetworkLoad { get; init; }    // 네트워크 사용 상대 점수 (0~100, 적응 스케일링)
    public float BatteryPercent { get; init; } // 배터리 잔량 % (없으면 100 취급)
    public bool IsCharging { get; init; }      // 충전 연결 여부
}
