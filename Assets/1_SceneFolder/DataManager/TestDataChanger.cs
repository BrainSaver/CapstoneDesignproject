using UnityEngine;
using UnityEngine.InputSystem;

public class TestDataChanger : MonoBehaviour
{
    private void Start()
    {
        // 게임이 시작될 때 스크립트가 켜졌는지부터 확인합니다.
        Debug.Log("TestDataChanger 스크립트가 정상적으로 켜졌습니다!");

        // 키보드 인식이 안 되는 상황인지 점검합니다.
        if (Keyboard.current == null)
        {
            Debug.LogWarning("경고: 유니티가 키보드를 인식하지 못하고 있습니다!");
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // 스페이스바 테스트
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("스페이스바가 눌렸습니다!");

            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.ModifyHP(-5);
                Debug.Log("체력 감소 함수 실행 완료!");
            }
            else
            {
                Debug.LogWarning("PlayerDataManager가 씬에 없습니다!");
            }
        }

        // G키 테스트
        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            Debug.Log("G키가 눌렸습니다!");

            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.AddGold(10);
                Debug.Log("골드 증가 함수 실행 완료!");
            }
        }

        // R키 테스트 (유물 획득)
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("R키가 눌렸습니다!");

            if (PlayerDataManager.Instance != null)
            {
                // 테스트용 유물: 전쟁 교본 (전투 시작 시 힘 +1)
                PlayerDataManager.Instance.AddRelic("warManual");
            }
        }
    }
}