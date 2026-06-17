using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class MapNodeUI : MonoBehaviour, IPointerDownHandler
{
    public MapNode nodeData;
    private Image nodeImage;
    private bool isClickable = false;

    [Header("Animation Settings")]
    private Vector3 originalScale;
    public float scaleSpeed = 5f;
    public float scaleAmount = 0.15f;

    [Header("Procedural Shatter Settings")]
    [Tooltip("이미지를 몇 등분으로 자를지 결정합니다 (예: 3이면 3x3 = 9조각)")]
    public int shatterGridSize = 3;
    [Tooltip("파편이 흩뿌려지는 힘의 크기입니다.")]
    public float explosionForce = 150f;

    private bool isBreaking = false;
    private GameObject shatterMarkContainer;

    private void Awake()
    {
        nodeImage = GetComponent<Image>();
        originalScale = transform.localScale;
    }

    private void Update()
    {
        if (isClickable && !isBreaking)
        {
            float scale = 1f + Mathf.Sin(Time.time * scaleSpeed) * scaleAmount;
            transform.localScale = originalScale * scale;
        }
    }

    public void Setup(MapNode data)
    {
        nodeData = data;
    }

    public void UpdateVisualState(bool isCurrent, bool isPassed, bool isClickable)
    {
        if (isBreaking) return;

        this.isClickable = isClickable;
        if (nodeImage == null) return;

        if (isPassed)
        {
            ShowShatteredMark();
        }
        else
        {
            if (shatterMarkContainer != null)
            {
                Destroy(shatterMarkContainer);
            }
            nodeImage.enabled = true;
            nodeImage.color = Color.white;
            transform.localScale = originalScale;
            transform.localRotation = Quaternion.identity;
        }
    }

    public void PlayBreakAnimation(System.Action onComplete)
    {
        isBreaking = true;
        isClickable = false;

        transform.localScale = originalScale;
        transform.localRotation = Quaternion.identity;

        CreateExplosionEffect();
        ShowShatteredMark();

        DOVirtual.DelayedCall(0.5f, () =>
        {
            onComplete?.Invoke();
        });
    }

    private void CreateExplosionEffect()
    {
        Sprite originalSprite = nodeImage.sprite;
        if (originalSprite == null || originalSprite.texture == null) return;

        Texture2D tex = originalSprite.texture;
        Rect texRect = originalSprite.rect;

        float texPieceWidth = texRect.width / shatterGridSize;
        float texPieceHeight = texRect.height / shatterGridSize;

        RectTransform originalRt = GetComponent<RectTransform>();
        float displayWidth = originalRt.rect.width;
        float displayHeight = originalRt.rect.height;
        float displayPieceWidth = displayWidth / shatterGridSize;
        float displayPieceHeight = displayHeight / shatterGridSize;

        GameObject container = new GameObject("ShatterExplosion");
        container.transform.SetParent(transform.parent, false);
        container.transform.position = transform.position;
        container.transform.SetAsLastSibling();

        for (int x = 0; x < shatterGridSize; x++)
        {
            for (int y = 0; y < shatterGridSize; y++)
            {
                Rect pieceRect = new Rect(texRect.x + x * texPieceWidth, texRect.y + y * texPieceHeight, texPieceWidth, texPieceHeight);
                Sprite pieceSprite = Sprite.Create(tex, pieceRect, new Vector2(0.5f, 0.5f), originalSprite.pixelsPerUnit);

                GameObject pieceGO = new GameObject($"ExpPiece_{x}_{y}");
                pieceGO.transform.SetParent(container.transform, false);

                Image pieceImage = pieceGO.AddComponent<Image>();
                pieceImage.sprite = pieceSprite;

                RectTransform rt = pieceGO.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(displayPieceWidth, displayPieceHeight);

                float offsetX = (x - (shatterGridSize - 1) / 2f) * displayPieceWidth;
                float offsetY = (y - (shatterGridSize - 1) / 2f) * displayPieceHeight;
                rt.anchoredPosition = new Vector2(offsetX, offsetY);

                Vector2 explosionDir = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
                float force = Random.Range(explosionForce * 0.4f, explosionForce);

                rt.DOAnchorPos(rt.anchoredPosition + explosionDir * force, 0.5f).SetEase(Ease.OutQuad);
                rt.DORotate(new Vector3(0, 0, Random.Range(-360f, 360f)), 0.5f, RotateMode.FastBeyond360);
                pieceImage.DOFade(0f, 0.5f).SetEase(Ease.InQuad);
            }
        }

        Destroy(container, 0.6f);
    }

    private void ShowShatteredMark()
    {
        if (shatterMarkContainer != null) return;

        nodeImage.enabled = false;

        Sprite originalSprite = nodeImage.sprite;
        if (originalSprite == null || originalSprite.texture == null) return;

        Texture2D tex = originalSprite.texture;
        Rect texRect = originalSprite.rect;

        float texPieceWidth = texRect.width / shatterGridSize;
        float texPieceHeight = texRect.height / shatterGridSize;

        RectTransform originalRt = GetComponent<RectTransform>();
        float displayWidth = originalRt.rect.width;
        float displayHeight = originalRt.rect.height;
        float displayPieceWidth = displayWidth / shatterGridSize;
        float displayPieceHeight = displayHeight / shatterGridSize;

        shatterMarkContainer = new GameObject("ShatterMarkContainer");
        shatterMarkContainer.transform.SetParent(transform, false);
        shatterMarkContainer.transform.SetAsFirstSibling();

        Random.State oldState = Random.state;
        Random.InitState((nodeData.pos.x * 100) + nodeData.pos.y);

        for (int x = 0; x < shatterGridSize; x++)
        {
            for (int y = 0; y < shatterGridSize; y++)
            {
                Rect pieceRect = new Rect(texRect.x + x * texPieceWidth, texRect.y + y * texPieceHeight, texPieceWidth, texPieceHeight);
                Sprite pieceSprite = Sprite.Create(tex, pieceRect, new Vector2(0.5f, 0.5f), originalSprite.pixelsPerUnit);

                GameObject pieceGO = new GameObject($"MarkPiece_{x}_{y}");
                pieceGO.transform.SetParent(shatterMarkContainer.transform, false);

                Image pieceImage = pieceGO.AddComponent<Image>();
                pieceImage.sprite = pieceSprite;

                // 💡 수정된 부분: 조각을 어둡게 만들던 코드를 제거하고, 기본 색상(흰색)을 적용하여 원본 밝기를 유지합니다.
                pieceImage.color = Color.white;

                RectTransform rt = pieceGO.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(displayPieceWidth, displayPieceHeight);

                float offsetX = (x - (shatterGridSize - 1) / 2f) * displayPieceWidth;
                float offsetY = (y - (shatterGridSize - 1) / 2f) * displayPieceHeight;

                float crackShiftX = Random.Range(-4f, 4f);
                float crackShiftY = Random.Range(-4f, 4f);
                float crackRot = Random.Range(-15f, 15f);

                rt.anchoredPosition = new Vector2(offsetX + crackShiftX, offsetY + crackShiftY);
                rt.localRotation = Quaternion.Euler(0, 0, crackRot);
            }
        }

        Random.state = oldState;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isClickable || isBreaking) return;

        if (MapManager.Instance != null)
        {
            MapManager.Instance.OnNodeClicked(this);
        }
    }
}