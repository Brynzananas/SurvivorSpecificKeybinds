using BepInEx;
using Rewired;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;
#pragma warning disable CS0618
#pragma warning restore CS0618
namespace SurvivorSpecificKeybinds;

public class SurvivorSpecificKeybindsButtonController : MonoBehaviour
{
    public string bodyName;
    public SurvivorSpecificKeybindsSelectorController survivorSpecificKeybindsSelectorController;
    public RawImage rawImage;
    public Color selectedColor;
    public Color unselectedColor;
    public void TestCall()
    {
        SurvivorSpecificKeybindsUtils.EnsureUserProfileDefaultMaps();
        UserProfile userProfile = SurvivorSpecificKeybindsUtils.GetUserProfile();
        if (userProfile == null) return;
        if (bodyName == "DefaultDefaultDefault")
        {
            userProfile.joystickMap = SurvivorSpecificKeybindsUtils.defaultJoystickMaps.TryGetValue(userProfile, out JoystickMap joystickMap) ? joystickMap : userProfile.joystickMap;
            userProfile.keyboardMap = SurvivorSpecificKeybindsUtils.defaultKeyboardMaps.TryGetValue(userProfile, out KeyboardMap keyboardMap) ? keyboardMap : userProfile.keyboardMap;
            userProfile.mouseMap = SurvivorSpecificKeybindsUtils.defaultMouseMaps.TryGetValue(userProfile, out MouseMap mouseMap) ? mouseMap : userProfile.mouseMap;
        }
        else
        {
            SurvivorSpecificKeybindsData survivorSpecificInputsData = SurvivorSpecificKeybindsUtils.EnsureUserProfileSurvivorSpecificKeybindsData(userProfile, bodyName);
            userProfile.keyboardMap = survivorSpecificInputsData.keyboardMap;
            userProfile.mouseMap = survivorSpecificInputsData.mouseMap;
            userProfile.joystickMap = survivorSpecificInputsData.joystickMap;
        }
        LocalUser localUser = LocalUserManager.GetFirstLocalUser();
        localUser.ApplyUserProfileBindingsToRewiredPlayer();
        MPEventSystem.RefreshAllControllerGlyphs();
        SurvivorSpecificKeybindsPlugin.currentSurvivorBody = bodyName;
    }
    public void Update()
    {
        if (bodyName == "DefaultDefaultDefault" && SurvivorSpecificKeybindsPlugin.currentSurvivorBody.IsNullOrWhiteSpace())
        {
            rawImage.color = selectedColor;
            return;
        }
        if (bodyName == SurvivorSpecificKeybindsPlugin.currentSurvivorBody)
        {
            rawImage.color = selectedColor;
        }
        else
        {
            rawImage.color = unselectedColor;
        }
    }
}
