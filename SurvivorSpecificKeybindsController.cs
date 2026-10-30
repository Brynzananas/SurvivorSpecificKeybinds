using Rewired;
using RoR2;
using RoR2.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#pragma warning disable CS0618
#pragma warning restore CS0618
namespace SurvivorSpecificKeybinds;

public class SurvivorSpecificKeybindsController : MonoBehaviour
{
    private bool init;
    public static SurvivorSpecificKeybindsController instance;
    public string selectedBodyName = "DefaultDefaultDefault";
    public Transform kbmHolder;
    private MPEventSystemLocator mPEventSystemLocator;
    public void Awake()
    {
        mPEventSystemLocator = GetComponent<MPEventSystemLocator>();
        instance = this;
        if (init) return;
        init = true;
        kbmHolder = transform.Find("SafeArea/SubPanelArea/SettingsSubPanel, Controls (M&KB)/Scroll View/Viewport/VerticalLayout");
        if (!kbmHolder) return;
        Transform transform2 = kbmHolder.Find("SettingsEntryButton, Bool (Invert X)");
        if (!transform2) return;
        GameObject gameObject2 = GameObject.Instantiate(transform2.gameObject, transform2.parent);
        gameObject2.transform.SetSiblingIndex(6);
        Transform transform5 = gameObject2.transform.Find("CarouselRect");
        if (transform5) GameObject.Destroy(transform5.gameObject);
        Transform transform6 = gameObject2.transform.Find("ButtonText");
        if (transform6) GameObject.Destroy(transform6.gameObject);
        GameObject gameObject3 = GameObject.Instantiate(SurvivorSpecificKeybindsPlugin.SurvivorIconsHolder, gameObject2.transform);
    }
    public void OnDisable()
    {
        if (!Run.instance || !mPEventSystemLocator || !mPEventSystemLocator.eventSystem || mPEventSystemLocator.eventSystem.localUser == null) return;
        SurvivorSpecificKeybindsUtils.SetSurvivorSpecificKeybinds(mPEventSystemLocator.eventSystem.localUser);
    }
}
