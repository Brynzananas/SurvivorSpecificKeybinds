using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;
#pragma warning disable CS0618
#pragma warning restore CS0618
namespace SurvivorSpecificKeybinds;

public class SurvivorSpecificKeybindsSelectorController : MonoBehaviour
{
    public SurvivorSpecificKeybindsController survivorSpecificInputsController;
    public GridLayoutGroup gridLayoutGroup;
    public Transform survivorIconsHolder;
    public GameObject survivorIconButtonPrefab;
    public float extraPadding = 10f;
    private LayoutElement layoutElement;
    private bool init;
    public void Awake()
    {
        if (!survivorSpecificInputsController) survivorSpecificInputsController = SurvivorSpecificKeybindsController.instance;
        layoutElement = transform.parent.GetComponent<LayoutElement>();
        if (!survivorIconButtonPrefab || !survivorIconsHolder || !RoR2Application.loadFinished || init) return;
        init = true;
        for (int i = 0; i < SurvivorCatalog.survivorCount; i++)
        {
            SurvivorDef survivorDef = SurvivorCatalog.survivorDefs[i];
            if (!survivorDef) continue;
            GameObject bodyPrefab = survivorDef.bodyPrefab;
            if (!bodyPrefab) continue;
            CharacterBody characterBody = bodyPrefab.GetComponent<CharacterBody>();
            if (!characterBody) continue;
            GameObject gameObject = GameObject.Instantiate(survivorIconButtonPrefab, survivorIconsHolder);
            SurvivorSpecificKeybindsButtonController survivorIconButtonController = gameObject.GetComponent<SurvivorSpecificKeybindsButtonController>();
            if (bodyPrefab.name == "DefaultDefaultDefault") SurvivorSpecificKeybindsPlugin.instance.Logger.LogWarning("Some individual decided to name their survivor body `DefaultDefaultDefault`. Expect issues");
            survivorIconButtonController.bodyName = bodyPrefab.name;
            if (survivorIconButtonController.rawImage) survivorIconButtonController.rawImage.texture = characterBody.portraitIcon;
            gameObject.SetActive(true);
        }
        survivorIconButtonPrefab.SetActive(true);
    }
    public void Update()
    {
        UpdateHeight();
    }
    public void UpdateHeight()
    {
        if (!layoutElement || !gridLayoutGroup) return;
        int childCount = 0;
        foreach (Transform child in gridLayoutGroup.transform)
        {
            if (child.gameObject.activeSelf) childCount++;
        }
        if (childCount == 0) return;
        int columns = 1;
        if (gridLayoutGroup.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            columns = Mathf.Max(1, gridLayoutGroup.constraintCount);
        }
        else
        {
            float availableWidth = ((RectTransform)gridLayoutGroup.transform).rect.width;
            float cellWidthWithSpacing = gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x;
            columns = Mathf.Max(1, Mathf.FloorToInt((availableWidth - gridLayoutGroup.padding.left - gridLayoutGroup.padding.right + gridLayoutGroup.spacing.x) / cellWidthWithSpacing));
        }
        int rows = Mathf.CeilToInt((float)childCount / columns);
        float totalHeight = gridLayoutGroup.padding.top +
                            gridLayoutGroup.padding.bottom +
                            (rows * gridLayoutGroup.cellSize.y) +
                                ((rows - 1) * gridLayoutGroup.spacing.y) +
                                extraPadding;
        layoutElement.preferredHeight = totalHeight;
        layoutElement.minHeight = totalHeight;
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }
}
