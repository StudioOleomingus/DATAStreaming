using UnityEngine;
public class HoverRaycastManager : MonoBehaviour
{
    [Tooltip("Max distance for the hover raycast")]
    [SerializeField] private float maxRayDistance = 100f;

    [Tooltip("Layer mask to filter which objects can be hovered")]
    [SerializeField] private LayerMask hoverLayerMask = ~0; 
    private HoverDataPanel currentHoveredPanel;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, hoverLayerMask))
        {
            HoverDataPanel panel = hit.collider.GetComponent<HoverDataPanel>();

            // If we hit a new panel
            if (panel != null && panel != currentHoveredPanel)
            {
                // Hide the old one
                if (currentHoveredPanel != null)
                    currentHoveredPanel.HidePanel();

                // Show the new one
                currentHoveredPanel = panel;
                currentHoveredPanel.ShowPanel();
            }
            // Ifhit something without a panel, hide current
            else if (panel == null && currentHoveredPanel != null)
            {
                currentHoveredPanel.HidePanel();
                currentHoveredPanel = null;
            }
        }
        else
        {
            // Raycast hit nothing — hide current panel
            if (currentHoveredPanel != null)
            {
                currentHoveredPanel.HidePanel();
                currentHoveredPanel = null;
            }
        }
    }
}
