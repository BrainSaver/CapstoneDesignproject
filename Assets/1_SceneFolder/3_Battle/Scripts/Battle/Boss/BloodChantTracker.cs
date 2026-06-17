/// <summary>
/// 피의 영창 디버프 상태를 추적하는 static 클래스.
/// 활성화 시 카드 사용 코스트만큼 플레이어 HP가 소모된다.
/// </summary>
public static class BloodChantTracker
{
    /// <summary>피의 영창이 활성화된 상태인지.</summary>
    public static bool IsActive { get; private set; } = false;

    /// <summary>피의 영창을 발동한다 (1턴 유지).</summary>
    public static void Activate()
    {
        IsActive = true;
        Logger.Log("[BloodChantTracker] 피의 영창 발동!");
    }

    /// <summary>턴 종료 시 호출. 1턴 유지이므로 즉시 해제한다.</summary>
    public static void Reset()
    {
        IsActive = false;
        Logger.Log("[BloodChantTracker] 피의 영창 해제.");
    }
}