using UnityEngine;
using UnityEngine.UI;

public class RestRoomManager : MonoBehaviour
{
    [Header("UI ��ư ����")]
    public Button healButton;
    public Button upgradeButton;

    private void Start()
    {
        // ��ư Ŭ�� �̺�Ʈ ����
        if (healButton != null)
        {
            healButton.onClick.AddListener(OnHealButtonClicked);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        }
    }

    // 1. �� ��ư Ŭ�� �� �۵��ϴ� ����
    private void OnHealButtonClicked()
    {
        // �ִ� ü���� 30%�� ����Ͽ� ������ �ݿø��մϴ�.
        int maxHp = PlayerDataManager.Instance.maxHP;
        int healAmount = Mathf.RoundToInt(maxHp * 0.3f);

        // ü���� ȸ����ŵ�ϴ�.
        PlayerDataManager.Instance.ModifyHP(healAmount);
        RelicManager.Instance?.OnRest();
        Debug.Log($"�޽�: �ִ� ü��({maxHp})�� 30%�� {healAmount}��ŭ ü���� ȸ���߽��ϴ�.");

        // �ൿ �Ϸ� ó�� �� �ε�� �̵�
        FinishRestRoomAction();
    }

    // 2. ��ȭ ��ư Ŭ�� �� �۵��ϴ� ����
    private void OnUpgradeButtonClicked()
    {
        RelicManager.Instance?.OnRest();
        // TODO: ���� �̰��� ī�� ��ȭ UI�� ���ų� ����� �����ϴ� ������ �߰��Ͻø� �˴ϴ�.
        Debug.Log("ī�� ��ȭ ����� ���� ������ �����Դϴ�.");

        // �ൿ �Ϸ� ó�� �� �ε�� �̵�
        FinishRestRoomAction();
    }

    /// <summary>
    /// �޽Ĺ濡���� �ൿ�� �������ϰ� �ε������ ���ư��ϴ�.
    /// </summary>
    private void FinishRestRoomAction()
    {
        // 1. ��Ÿ�� ���� ���׸� ���� ���� ��ư���� ��� ��Ȱ��ȭ�մϴ�.
        if (healButton != null) healButton.interactable = false;
        if (upgradeButton != null) upgradeButton.interactable = false;

        // 2. SceneFlowManager�� Ȱ���Ͽ� �ε�� ������ �̵��մϴ�.
        if (SceneFlowManager.Instance != null)
        {
            SceneFlowManager.Instance.LoadScene(SceneType.RoadMap);
        }
        else
        {
            Debug.LogError("[RestRoomManager] SceneFlowManager�� ã�� �� ���� RoadMap���� �̵��� �� �����ϴ�.");
        }
    }
}