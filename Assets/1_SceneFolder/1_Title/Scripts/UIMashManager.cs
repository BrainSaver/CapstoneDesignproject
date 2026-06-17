using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using DG.Tweening;

public class UIMashManager : MonoBehaviour
{
    [Header("�ָ� (�Ϲ� 2D ��������Ʈ)")]
    public Transform leftFist;
    public Transform rightFist;

    [Header("��ư UI ����")]
    public RectTransform startButton;
    public RectTransform optionButton;
    public RectTransform continueButton;
    public RectTransform creditButton;

    [Header("���� ����")]
    public float punchSpeed = 0.2f;
    public float scatterSpeed = 2.5f;
    public float scatterDistance = 2000f;
    public float returnSpeed = 1.5f;

    [Header("��� Ÿ�� ����")]
    public float emptyPunchDepth = 10f;

    private Vector3 originRightFistPos;
    private Vector3 originLeftFistPos;
    private bool isLeftPunchNext = false;

    // ���� �ִϸ��̼� ���� �ߺ� ������ ���� ���� �÷���
    private bool isResetting = false;

    private Dictionary<RectTransform, Vector2> initialAnchoredPositions = new Dictionary<RectTransform, Vector2>();
    private VerticalLayoutGroup layoutGroup;

    private void Start()
    {
        // ==========================================
        // ���� ����: �ʱ� ��ġ ���� �� ���̾ƿ� ���� ������Ʈ
        // ==========================================
        Canvas.ForceUpdateCanvases();

        layoutGroup = startButton.parent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            // VerticalLayoutGroup�� ��ġ�� ��� �����ϵ��� �����մϴ�.
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
        }

        // ������ �Ϸ�� ��Ȯ�� ��ǥ�� �����մϴ�.
        SaveInitialState(startButton);
        SaveInitialState(optionButton);
        SaveInitialState(continueButton);
        SaveInitialState(creditButton);

        if (rightFist != null) originRightFistPos = rightFist.position;
        if (leftFist != null) originLeftFistPos = leftFist.position;
    }

    private void SaveInitialState(RectTransform btn)
    {
        if (btn != null) initialAnchoredPositions[btn] = btn.anchoredPosition;
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // ��ư�� ���ڸ��� ���ƿ��� �߿��� Ÿ���� �����Ͽ� ��ǥ�� ������ �ʰ� �մϴ�.
            if (!EventSystem.current.IsPointerOverGameObject() && !isResetting)
            {
                ExecuteEmptyPunch(Mouse.current.position.ReadValue());
            }
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ResetButtonsAnimated();
        }
    }

    private void ResetButtonsAnimated()
    {
        if (isResetting) return;
        isResetting = true;

        startButton.DOKill();
        optionButton.DOKill();
        continueButton.DOKill();
        creditButton.DOKill();

        RestoreButtonAnimated(startButton);
        RestoreButtonAnimated(optionButton);
        RestoreButtonAnimated(continueButton);
        RestoreButtonAnimated(creditButton);

        // ��ư�� �Ϻ��� ���ڸ��� ������ ���Ŀ� ���̾ƿ� �׷��� �մϴ�.
        DOVirtual.DelayedCall(returnSpeed + 0.05f, () =>
        {
            if (layoutGroup != null) layoutGroup.enabled = true;
            isResetting = false;
            Debug.Log("��� ��ư�� ���ڸ��� ��Ȯ�� ���ƿԽ��ϴ�.");
        });
    }

    private void RestoreButtonAnimated(RectTransform btn)
    {
        if (btn == null) return;

        if (!btn.gameObject.activeSelf)
        {
            btn.gameObject.SetActive(true);
        }

        btn.DOScale(Vector3.one, returnSpeed).SetEase(Ease.OutQuart);

        if (initialAnchoredPositions.ContainsKey(btn))
        {
            btn.DOAnchorPos(initialAnchoredPositions[btn], returnSpeed).SetEase(Ease.OutQuart);
            btn.DORotate(Vector3.zero, returnSpeed).SetEase(Ease.OutQuart);
        }
    }

    private void ExecuteEmptyPunch(Vector2 mouseScreenPos)
    {
        Transform activeFist = isLeftPunchNext ? leftFist : rightFist;
        Vector3 activeOriginPos = isLeftPunchNext ? originLeftFistPos : originRightFistPos;
        isLeftPunchNext = !isLeftPunchNext;

        float zDepth = Mathf.Abs(Camera.main.transform.position.z) + emptyPunchDepth;
        Vector3 screenPosWithZ = new Vector3(mouseScreenPos.x, mouseScreenPos.y, zDepth);
        Vector3 targetPosition = Camera.main.ScreenToWorldPoint(screenPosWithZ);

        activeFist.DOMove(targetPosition, punchSpeed).SetEase(Ease.InBack).OnComplete(() =>
        {
            if (Camera.main != null) Camera.main.transform.DOShakePosition(0.1f, strength: 0.2f);
            activeFist.DOMove(activeOriginPos, punchSpeed).SetEase(Ease.OutQuad);
        });
    }

    public void OnClickStart() { if (!isResetting) ExecutePunch(startButton, () => { Debug.Log("���� ����!"); }); }
    public void OnClickContinue() { if (!isResetting) ExecutePunch(continueButton, () => { Debug.Log("�̾��ϱ�!"); }); }
    public void OnClickOption() { if (!isResetting) ExecutePunch(optionButton, () => { Debug.Log("�ɼ� ����!"); }); }
    public void OnClickCredit() { if (!isResetting) ExecutePunch(creditButton, () => { Debug.Log("ũ���� ����!"); }); }

    private void ExecutePunch(RectTransform targetButton, Action onActionExecute)
    {
        Canvas canvas = targetButton.GetComponentInParent<Canvas>();
        Vector2 screenPos;

        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            screenPos = targetButton.position;
        else
            screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, targetButton.position);

        Transform activeFist = isLeftPunchNext ? leftFist : rightFist;
        Vector3 activeOriginPos = isLeftPunchNext ? originLeftFistPos : originRightFistPos;
        isLeftPunchNext = !isLeftPunchNext;

        float zDepth = Mathf.Abs(Camera.main.transform.position.z) + emptyPunchDepth;
        Vector3 screenPosWithZ = new Vector3(screenPos.x, screenPos.y, zDepth);
        Vector3 targetPosition = Camera.main.ScreenToWorldPoint(screenPosWithZ);

        activeFist.DOMove(targetPosition, punchSpeed).SetEase(Ease.InBack).OnComplete(() =>
        {
            SmashEffect(targetButton);
            ScatterOtherButtons(targetButton);
            activeFist.DOMove(activeOriginPos, punchSpeed).SetEase(Ease.OutQuad);

            DOVirtual.DelayedCall(scatterSpeed, () =>
            {
                if (targetButton.gameObject.activeSelf == false && targetButton.localScale.x < 0.1f)
                    onActionExecute?.Invoke();
            });
        });
    }

    private void SmashEffect(RectTransform button)
    {
        button.DOScale(Vector3.zero, 0.2f).OnComplete(() => button.gameObject.SetActive(false));
        if (Camera.main != null) Camera.main.transform.DOShakePosition(0.2f, strength: 0.5f);
    }

    private void ScatterOtherButtons(RectTransform punchedButton)
    {
        if (layoutGroup != null) layoutGroup.enabled = false;

        RectTransform[] leftButtons = { startButton, optionButton };
        RectTransform[] rightButtons = { continueButton, creditButton };

        foreach (RectTransform btn in leftButtons)
        {
            if (btn != punchedButton)
            {
                btn.DOAnchorPosX(btn.anchoredPosition.x - scatterDistance, scatterSpeed).SetEase(Ease.OutQuart)
                   .OnComplete(() => btn.gameObject.SetActive(false));
                btn.DORotate(new Vector3(0, 0, 720f), scatterSpeed, RotateMode.FastBeyond360).SetEase(Ease.OutQuart);
            }
        }

        foreach (RectTransform btn in rightButtons)
        {
            if (btn != punchedButton)
            {
                btn.DOAnchorPosX(btn.anchoredPosition.x + scatterDistance, scatterSpeed).SetEase(Ease.OutQuart)
                   .OnComplete(() => btn.gameObject.SetActive(false));
                btn.DORotate(new Vector3(0, 0, -720f), scatterSpeed, RotateMode.FastBeyond360).SetEase(Ease.OutQuart);
            }
        }
    }
}