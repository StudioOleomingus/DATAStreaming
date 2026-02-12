using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Collider))]
public class HoverDataPanel : MonoBehaviour
{
    [Header("UI Prefab")]
    [Tooltip("Drag your World Space Canvas prefab here")]
    [SerializeField] private GameObject uiPrefab;

    [Header("Prefab Object Names")]
    [SerializeField] private string nameTextObjectName = "NameText";
    [SerializeField] private string desc1TextObjectName = "Desc1Text";
    [SerializeField] private string desc2TextObjectName = "Desc2Text";
    [SerializeField] private string yearTextObjectName = "YearText";
    [SerializeField] private string imageDisplayObjectName = "ImageDisplay";
    [SerializeField] private string imageCounterObjectName = "ImageCounter";

    [Header("Positioning")]
    [Tooltip("Offset above the object where the panel spawns")]
    [SerializeField] private Vector3 uiOffset = new Vector3(0f, 2.5f, 0f);

    [Tooltip("If true, the panel always faces the camera")]
    [SerializeField] private bool billboardToCamera = true;

    private GameObject uiInstance;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI desc1Text;
    private TextMeshProUGUI desc2Text;
    private TextMeshProUGUI yearText;
    private Image imageDisplay;
    private TextMeshProUGUI imageCounterText;

    private List<ExcelDataEntry> entries;
    private int currentYearIndex = 0;
    private int currentImageIndex = 0;
    private bool isHovered = false;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        LoadData();
        InstantiatePrefab();
    }

    private void LoadData()
    {
        if (ExcelDataLoader.Instance == null)
        {
            Debug.LogError("[HoverDataPanel] ExcelDataLoader instance not found in scene.");
            return;
        }

        entries = ExcelDataLoader.Instance.GetEntriesForTag(gameObject.tag);

        if (entries == null || entries.Count == 0)
            Debug.LogWarning($"[HoverDataPanel] No data found for tag '{gameObject.tag}' on '{gameObject.name}'.");
    }

    private void InstantiatePrefab()
    {
        if (uiPrefab == null)
        {
            Debug.LogError($"[HoverDataPanel] No UI Prefab assigned on '{gameObject.name}'.");
            return;
        }

        uiInstance = Instantiate(uiPrefab, transform);
        uiInstance.transform.localPosition = uiOffset;
        uiInstance.name = $"HoverUI_{gameObject.name}";

        // Find text components
        nameText = FindTextInChildren(uiInstance, nameTextObjectName);
        desc1Text = FindTextInChildren(uiInstance, desc1TextObjectName);
        desc2Text = FindTextInChildren(uiInstance, desc2TextObjectName);
        yearText = FindTextInChildren(uiInstance, yearTextObjectName);
        imageCounterText = FindTextInChildren(uiInstance, imageCounterObjectName);

        // Find the Image component
        imageDisplay = FindImageInChildren(uiInstance, imageDisplayObjectName);

        if (nameText == null) Debug.LogWarning($"[HoverDataPanel] '{nameTextObjectName}' not found in prefab on '{gameObject.name}'.");
        if (desc1Text == null) Debug.LogWarning($"[HoverDataPanel] '{desc1TextObjectName}' not found in prefab on '{gameObject.name}'.");
        if (desc2Text == null) Debug.LogWarning($"[HoverDataPanel] '{desc2TextObjectName}' not found in prefab on '{gameObject.name}'.");
        if (yearText == null) Debug.LogWarning($"[HoverDataPanel] '{yearTextObjectName}' not found in prefab on '{gameObject.name}'.");
        if (imageDisplay == null) Debug.LogWarning($"[HoverDataPanel] '{imageDisplayObjectName}' not found in prefab on '{gameObject.name}'. Images won't display.");

        uiInstance.SetActive(false);
    }

    private void Update()
    {
        if (!isHovered || entries == null || entries.Count == 0) return;
//billbord
        if (billboardToCamera && mainCamera != null && uiInstance != null)
            uiInstance.transform.rotation = mainCamera.transform.rotation;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            currentYearIndex = (currentYearIndex + 1) % entries.Count;
            currentImageIndex = 0; // Reset image index when changing year
            UpdateUI();
        }
        else if (scroll < 0f)
        {
            currentYearIndex = (currentYearIndex - 1 + entries.Count) % entries.Count;
            currentImageIndex = 0;
            UpdateUI();
        }

        // Left click — cycle images
        if (Input.GetMouseButtonDown(0))
        {
            ExcelDataEntry entry = entries[currentYearIndex];
            if (entry.imageFileNames.Count > 1)
            {
                currentImageIndex = (currentImageIndex + 1) % entry.imageFileNames.Count;
                UpdateImage(entry);
            }
        }
    }

    public void ShowPanel()
    {
        if (entries == null || entries.Count == 0 || uiInstance == null) return;

        isHovered = true;
        currentImageIndex = 0;
        uiInstance.SetActive(true);
        UpdateUI();
    }

    public void HidePanel()
    {
        isHovered = false;
        if (uiInstance != null)
            uiInstance.SetActive(false);
    }

    private void UpdateUI()
    {
        if (entries == null || entries.Count == 0) return;

        ExcelDataEntry entry = entries[currentYearIndex];

        if (nameText != null) nameText.text = entry.name;
        if (desc1Text != null) desc1Text.text = entry.description1;
        if (desc2Text != null) desc2Text.text = entry.description2;

        // Year indicator
        if (yearText != null)
        {
            if (entries.Count > 1)
                yearText.text = $"\u25C4  {entry.year}  \u25BA   ({currentYearIndex + 1}/{entries.Count})  [Scroll to change year]";
            else
                yearText.text = $"{entry.year}";
        }

        // Image
        UpdateImage(entry);
    }

    private void UpdateImage(ExcelDataEntry entry)
    {
        if (imageDisplay == null) return;

        if (entry.imageFileNames.Count == 0)
        {
            // No images for this entry — hide the image area
            imageDisplay.gameObject.SetActive(false);
            if (imageCounterText != null)
                imageCounterText.gameObject.SetActive(false);
            return;
        }

        imageDisplay.gameObject.SetActive(true);

        // Clamp index 
        currentImageIndex = Mathf.Clamp(currentImageIndex, 0, entry.imageFileNames.Count - 1);

        string fileName = entry.imageFileNames[currentImageIndex];
        Sprite sprite = ExcelDataLoader.Instance.GetSprite(fileName);

        if (sprite != null)
        {
            imageDisplay.sprite = sprite;
            imageDisplay.preserveAspect = true;
            imageDisplay.color = Color.white;
        }
        else
        {
            imageDisplay.sprite = null;
            imageDisplay.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            Debug.LogWarning($"[HoverDataPanel] Sprite not found for '{fileName}'. Is it in StreamingAssets?");
        }

        if (imageCounterText != null)
        {
            if (entry.imageFileNames.Count > 1)
            {
                imageCounterText.gameObject.SetActive(true);
                imageCounterText.text = $"Image {currentImageIndex + 1}/{entry.imageFileNames.Count}  [Click to browse]";
            }
            else
            {
                imageCounterText.gameObject.SetActive(false);
            }
        }
    }

    private TextMeshProUGUI FindTextInChildren(GameObject root, string objectName)
    {
        TextMeshProUGUI[] allTexts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in allTexts)
        {
            if (tmp.gameObject.name == objectName)
                return tmp;
        }
        return null;
    }

    private Image FindImageInChildren(GameObject root, string objectName)
    {
        Image[] allImages = root.GetComponentsInChildren<Image>(true);
        foreach (var img in allImages)
        {
            if (img.gameObject.name == objectName)
                return img;
        }
        return null;
    }

    private void OnDestroy()
    {
        if (uiInstance != null)
            Destroy(uiInstance);
    }
}
