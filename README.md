# Mood Ring 위젯 (WPF / .NET 8)

가벼운 시스템 상태 감성 표시 위젯. CPU / 메모리 / 디스크 / 네트워크 / 배터리 정보를 가중 합산해 0~100 점수(CompositeScore)를 만들고, 부드러운 색상 링(HSV 보간)으로 "무드"를 표현합니다.

> 후원 / Support: 토스뱅크 1001-2269-0600 (개발 지속에 큰 힘이 됩니다.)

---
## 주요 기능
- 프레임리스, 투명, TopMost 원형 링 위젯 (드래그 이동 / 크기(휠) 조절 / 잠금)
- CPU(40) + Memory(30) + Disk(15) + Network(15) 가중 종합 점수
- 배터리: 충전 중 -5 / 20% 미만 +10 보정 (옵션)
- EMA(α=0.2) 스무딩으로 급격한 변동 완화
- 색상 맵: 청록(#2dd4bf) → 라임(#84cc16) → 주황(#fb923c) → 레드(#ef4444)
- 표시 모드: Score / Emoji / Text (더블클릭 순환)
- 자동 페이드: 마우스 멀어지면 투명도 0.7, 근접 시 1.0
- 시스템 트레이 아이콘 및 컨텍스트 메뉴 (표시/숨김, 크기 프리셋, 잠금, 종료)
- %AppData%/MoodRing/settings.json 에 설정 저장 (위치, 크기, 모드 등)

## 구조
```
Mood_Ring/
 ├─ Models/        # Settings, SystemSnapshot
 ├─ Services/      # Metrics, Mood(점수/색), Settings 저장, Tray
 ├─ ViewModels/    # RingViewModel (바인딩 상태)
 ├─ Views/         # RingWindow (투명 위젯 창)
 ├─ Controls/      # MoodRingControl (아크/펄스/Glow)
 ├─ Helpers/       # ColorInterpolation (HSV 보간)
 ├─ README.md
 └─ LICENSE
```

## Composite Score 계산 로직
1. 메트릭 수집: PerformanceCounter + PowerStatus
2. 가중합: CPU*0.40 + MEM*0.30 + DISK*0.15 + NET*0.15
3. 배터리 보정: (충전 -5) / (20% 미만 +10)
4. 0~100 클램프
5. EMA 적용: ema = α * current + (1-α) * ema_prev (초기 prev는 current)

## 색상 전환 (HSV 보간)
- 구간별 Anchor 색상 배열로 Value 위치 파악 후 HSV 선형 보간
- ColorInterpolation.LerpHsv → H 최단 거리 회전
- SolidColorBrush Freeze() 적용으로 UI 스레드 부담 최소화

## 빌드 & 실행
.NET 8 SDK 설치 후:
```
dotnet build
bin/Debug/net8.0-windows/Mood_Ring.exe
```
(또는 IDE에서 실행)

## 향후 TODO
- AutostartService (윈도우 시작 시 자동 실행)
- 듀얼 모드(차분/집중) 색상/가중치 프로파일 전환
- 배터리 잔여 시간 추정 및 표시 배지
- 네트워크/디스크 예외 상황 재초기화 로직 (절전 복귀 등)
- 색상 전환 애니메이션 (ColorAnimation, eased)
- 글로우 강도 점수 기반 동적 반영
- 접근성: 고대비 모드 감지 후 대비 최적화

## 기여
PR / Issue 환영. 경량 & 무상태 지향. 큰 외부 의존성 추가는 사전 논의 권장.

## 라이선스
본 프로젝트는 커스텀 비상업적 사용 허가서(Non-Commercial License)를 따릅니다. 상업적 / 영리 목적(유료 판매, 유료 서비스 번들, 광고 수익 목적 재배포 등)으로 사용할 수 없습니다. 오픈소스/개인/교육/내부 도구 용도로는 출처(README 링크) 표시 후 자유롭게 사용/수정 가능합니다. 자세한 내용은 LICENSE 파일을 참고하세요.

## 후원
토스뱅크 1001-2269-0600
작은 후원도 개발 유지와 기능 개선에 큰 도움이 됩니다. 감사합니다!
