using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Rewired;
using Rewired.Data;
using Rewired.Data.Mapping;
using RoR2;
using RoR2.UI;
using RoR2.UI.MainMenu;
using RoR2BepInExPack.Utilities;
using SurvivorSpecificKeybinds;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Zio;
using Zio.FileSystems;
using static System.Runtime.CompilerServices.RuntimeHelpers;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]
#pragma warning disable CS0618
#pragma warning restore CS0618
namespace SurvivorSpecificKeybinds;

[BepInPlugin(ModGuid, ModName, ModVer)]
[System.Serializable]
public class SurvivorSpecificKeybindsPlugin : BaseUnityPlugin
{
    public const string ModGuid = "com.brynzananas.survivorspecifickeybinds";
    public const string ModName = "Survivor Specific Keybinds";
    public const string ModVer = "0.1.0";
    public static AssetBundle assetBundle;
    public static GameObject SurvivorIconsHolder;
    public static SurvivorSpecificKeybindsPlugin instance;
    public static string currentSurvivorBody;
    public static FixedConditionalWeakTable<UserProfile, SurvivorSpecificKeybindsSave> keyValuePairs = [];
    public void Awake()
    {
        instance = this;
        Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/SettingsPanelTitle.prefab").WaitForCompletion().AddComponent<SurvivorSpecificKeybindsController>();
        assetBundle = AssetBundle.LoadFromFileAsync(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(this.Info.Location), "assetbundles", "survivorspecificsettingsassets")).assetBundle;
        SurvivorIconsHolder = assetBundle.LoadAsset<GameObject>("Assets/SurvivorSpecificSettings/SurvivorIconsHolder.prefab");
        SetHooks();
    }
    private static bool hooksSet;
    private static void SetHooks()
    {
        if (hooksSet) return;
        hooksSet = true;
        IL.RoR2.LocalUser.RebuildControlChain += LocalUser_RebuildControlChain;
        On.RoR2.SaveSystemSteam.StartSave += SaveSystemSteam_StartSave;
        IL.RoR2.SaveSystem.LoadUserProfiles += SaveSystem_LoadUserProfiles;
        On.RoR2.SaveSystem.LoadUserProfiles += SaveSystem_LoadUserProfiles1;
    }
    private static void SaveSystem_LoadUserProfiles1(On.RoR2.SaveSystem.orig_LoadUserProfiles orig, SaveSystem self)
    {
        orig(self);
        foreach (var pair in keyValuePairs) SurvivorSpecificKeybindsUtils.inputsByBodyNames[pair.Key] = pair.Value.keyValuePairs;
    }

    private static void SaveSystem_LoadUserProfiles(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int locId = 6;
        if (!c.TryGotoNext(MoveType.After,
        [
            x => x.MatchLdloc(out locId),
            x => x.MatchCall<SaveSystem>(nameof(SaveSystem.AddLoadedUserProfile))
        ]))
        {
            instance.Logger.LogError("IL Hook " + il.Method.Name + " failed!");
        }
        else
        {
            c.Emit(OpCodes.Ldloc, locId);
            c.EmitDelegate(UserProfileOrPvpBoss);
        }
    }
    private static void UserProfileOrPvpBoss(UserProfile userProfile)
    {
        FileSystem fileSystem = RoR2Application.cloudStorage;
        string directory = "/UserProfiles/" + userProfile.fileName.CleanString();
        instance.Logger.LogInfo("Reading path: " + directory);
        if (!fileSystem.DirectoryExists(directory))
        {
            fileSystem.CreateDirectory(directory);
        }
        foreach (UPath uPath in fileSystem.EnumeratePaths(directory))
        {
            if (!fileSystem.FileExists(uPath)) continue;
            instance.Logger.LogInfo("fullpath: " + uPath.FullName);
            if (!uPath.FullName.EndsWith("SurvivorSpecificKeybinds.xml")) continue;
            try
            {
                Stream stream = fileSystem.OpenFile(uPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (stream == null) continue;
                try
                {
                    TextReader textReader = new StreamReader(stream, Encoding.UTF8);
                    if (textReader == null) continue;
                    try
                    {
                        XDocument xDocument = XDocument.Load(textReader);
                        if (xDocument == null) continue;
                        SurvivorSpecificKeybindsSave survivorSpecificKeybindsSave = SurvivorSpecificKeybindsSave.FromXml(xDocument);
                        keyValuePairs[userProfile] = survivorSpecificKeybindsSave;
                    }
                    catch (Exception e)
                    {
                        instance.Logger.LogError(e);
                    }
                }
                catch (Exception e)
                {
                    instance.Logger.LogError(e);
                }
            }
            catch (Exception e)
            {
                instance.Logger.LogError(e);
            }
        }
    }
    private static void SaveSystemSteam_StartSave(On.RoR2.SaveSystemSteam.orig_StartSave orig, SaveSystemSteam self, UserProfile userProfile, bool blocking)
    {
        SurvivorSpecificKeybindsUtils.EnsureUserProfileDefaultMaps(userProfile);
        KeyboardMap keyboardMap = userProfile.keyboardMap;
        MouseMap mouseMap = userProfile.mouseMap;
        JoystickMap joystickMap = userProfile.joystickMap;
        userProfile.keyboardMap = SurvivorSpecificKeybindsUtils.defaultKeyboardMaps[userProfile];
        userProfile.mouseMap = SurvivorSpecificKeybindsUtils.defaultMouseMaps[userProfile];
        userProfile.joystickMap = SurvivorSpecificKeybindsUtils.defaultJoystickMaps[userProfile];
        orig(self, userProfile, blocking);
        userProfile.keyboardMap = keyboardMap;
        userProfile.mouseMap = mouseMap;
        userProfile.joystickMap = joystickMap;
        if (!keyValuePairs.TryGetValue(userProfile, out SurvivorSpecificKeybindsSave survivorSpecificKeybindsSave)) return;
        if (survivorSpecificKeybindsSave.keyValuePairs == null || survivorSpecificKeybindsSave.keyValuePairs.Count <= 0) return;
        FileOutput fileOutput = SurvivorSpecificKeybindsUtils.GetFileOutput(userProfile);
        Task task = null;
        task = new Task(PayloadGeneratorAction);
        self.AddActiveTask(task);
        task.Start(TaskScheduler.Default);
        if (blocking)
        {
            task.Wait();
            self.ProcessFileOutputQueue();
        }
        void PayloadGeneratorAction()
        {
            try
            {
                FileIoIndicatorManager.IncrementActiveWriteCount();
                XDocument xDocument = survivorSpecificKeybindsSave.ToXml();
                SurvivorSpecificKeybindsUtils.SaveFileOutput(self, xDocument, fileOutput);
            }
            catch (Exception)
            {
                RoR2Application.onNextUpdate += delegate
                {
                };
                throw;
            }
            finally
            {
                FileIoIndicatorManager.DecrementActiveWriteCount();
                self.RemoveActiveTask(task);
            }
        }
    }
    private static void LocalUser_RebuildControlChain(ILContext il)
    {
        ILCursor c = new ILCursor(il);
        int locId = 2;
        ILLabel iLLabel = null;
        if (c.TryGotoNext(
            MoveType.Before,
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<LocalUser>(nameof(LocalUser.onBodyChanged))
        ))
        {
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(OnBodyChanged);
        }
        else
        {
            instance.Logger.LogError("IL Hook " + il.Method.Name + " failed!");
        }
    }
    private static void OnBodyChanged(LocalUser localUser)
    {
        UserProfile userProfile = localUser.userProfile;
        if (userProfile == null) return;
        SurvivorSpecificKeybindsUtils.EnsureUserProfileDefaultMaps(userProfile);
        if (!localUser.cachedBody)
        {
            userProfile.keyboardMap = SurvivorSpecificKeybindsUtils.defaultKeyboardMaps[userProfile];
            userProfile.mouseMap = SurvivorSpecificKeybindsUtils.defaultMouseMaps[userProfile];
            userProfile.joystickMap = SurvivorSpecificKeybindsUtils.defaultJoystickMaps[userProfile];
            localUser.ApplyUserProfileBindingsToRewiredPlayer();
            MPEventSystem.RefreshAllControllerGlyphs();
        }
        string bodyName = localUser.cachedBody.name.Replace("(Clone)", "");
        if (SurvivorSpecificKeybindsUtils.TryGetUserProfileSurvivorSpecificInputsData(userProfile, bodyName, out SurvivorSpecificKeybindsData survivorSpecificInputsData))
        {
            userProfile.keyboardMap = survivorSpecificInputsData.keyboardMap;
            userProfile.joystickMap = survivorSpecificInputsData.joystickMap;
            userProfile.mouseMap = survivorSpecificInputsData.mouseMap;
            localUser.ApplyUserProfileBindingsToRewiredPlayer();
            MPEventSystem.RefreshAllControllerGlyphs();
        }
        else
        {
            userProfile.keyboardMap = SurvivorSpecificKeybindsUtils.defaultKeyboardMaps[userProfile];
            userProfile.mouseMap = SurvivorSpecificKeybindsUtils.defaultMouseMaps[userProfile];
            userProfile.joystickMap = SurvivorSpecificKeybindsUtils.defaultJoystickMaps[userProfile];
            localUser.ApplyUserProfileBindingsToRewiredPlayer();
            MPEventSystem.RefreshAllControllerGlyphs();
        }
    }
}
