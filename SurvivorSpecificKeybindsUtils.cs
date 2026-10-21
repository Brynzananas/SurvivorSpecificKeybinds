using BepInEx.Configuration;
using Rewired;
using RoR2;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using UnityEngine;
#pragma warning disable CS0618
#pragma warning restore CS0618
namespace SurvivorSpecificKeybinds;

public static class SurvivorSpecificKeybindsUtils
{
    public static FixedConditionalWeakTable<UserProfile, KeyboardMap> defaultKeyboardMaps = [];
    public static FixedConditionalWeakTable<UserProfile, MouseMap> defaultMouseMaps = [];
    public static FixedConditionalWeakTable<UserProfile, JoystickMap> defaultJoystickMaps = [];
    public static FixedConditionalWeakTable<UserProfile, Dictionary<string, SurvivorSpecificKeybindsData>> inputsByBodyNames = [];
    public static void EnsureUserProfileDefaultMaps() => EnsureUserProfileDefaultMaps(GetUserProfile());
    public static void EnsureUserProfileDefaultMaps(UserProfile userProfile)
    {
        EnsureUserProfileDefaultKeyboardMap(userProfile);
        EnsureUserProfileDefaultMouseMap(userProfile);
        EnsureUserProfileDefaultJoystickMap(userProfile);
    }
    public static KeyboardMap EnsureUserProfileDefaultKeyboardMap() => EnsureUserProfileDefaultKeyboardMap(GetUserProfile());
    public static KeyboardMap EnsureUserProfileDefaultKeyboardMap(UserProfile userProfile)
    {
        if (userProfile == null) return null;
        if (!defaultKeyboardMaps.TryGetValue(userProfile, out KeyboardMap keyboardMap))
        {
            keyboardMap = userProfile.keyboardMap;
            defaultKeyboardMaps.Add(userProfile, keyboardMap);
        }
        return keyboardMap;
    }
    public static MouseMap EnsureUserProfileDefaultMouseMap() => EnsureUserProfileDefaultMouseMap(GetUserProfile());
    public static MouseMap EnsureUserProfileDefaultMouseMap(UserProfile userProfile)
    {
        if (userProfile == null) return null;
        if (!defaultMouseMaps.TryGetValue(userProfile, out MouseMap mouseMap))
        {
            mouseMap = userProfile.mouseMap;
            defaultMouseMaps.Add(userProfile, mouseMap);
        }
        return mouseMap;
    }
    public static JoystickMap EnsureUserProfileDefaultJoystickMap() => EnsureUserProfileDefaultJoystickMap(GetUserProfile());
    public static JoystickMap EnsureUserProfileDefaultJoystickMap(UserProfile userProfile)
    {
        if (userProfile == null) return null;
        if (!defaultJoystickMaps.TryGetValue(userProfile, out JoystickMap joyStickMap))
        {
            joyStickMap = userProfile.joystickMap;
            defaultJoystickMaps.Add(userProfile, joyStickMap);
        }
        return joyStickMap;
    }
    public static SurvivorSpecificKeybindsData EnsureUserProfileSurvivorSpecificKeybindsData(string bodyName) => EnsureUserProfileSurvivorSpecificKeybindsData(GetUserProfile(), bodyName);
    public static SurvivorSpecificKeybindsData EnsureUserProfileSurvivorSpecificKeybindsData(UserProfile userProfile, string bodyName)
    {
        if (userProfile == null) return null;
        if (!inputsByBodyNames.TryGetValue(userProfile, out Dictionary<string, SurvivorSpecificKeybindsData> dict))
        {
            dict = [];
            dict[bodyName] = new SurvivorSpecificKeybindsData
            {
                bodyName = bodyName,
                joystickMap = defaultJoystickMaps.TryGetValue(userProfile, out JoystickMap joystickMap) ? joystickMap.Clone() :  userProfile.joystickMap.Clone(),
                keyboardMap = defaultKeyboardMaps.TryGetValue(userProfile, out KeyboardMap keyboardMap) ? keyboardMap.Clone() : userProfile.keyboardMap.Clone(),
                mouseMap = defaultMouseMaps.TryGetValue(userProfile, out MouseMap mouseMap) ? mouseMap.Clone() : userProfile.mouseMap.Clone(),
            };
            inputsByBodyNames[userProfile] = dict;
        }
        if (!dict.TryGetValue(bodyName, out SurvivorSpecificKeybindsData survivorSpecificInputsData))
        {
            survivorSpecificInputsData = new SurvivorSpecificKeybindsData
            {
                bodyName = bodyName,
                joystickMap = defaultJoystickMaps.TryGetValue(userProfile, out JoystickMap joystickMap) ? joystickMap.Clone() : userProfile.joystickMap.Clone(),
                keyboardMap = defaultKeyboardMaps.TryGetValue(userProfile, out KeyboardMap keyboardMap) ? keyboardMap.Clone() : userProfile.keyboardMap.Clone(),
                mouseMap = defaultMouseMaps.TryGetValue(userProfile, out MouseMap mouseMap) ? mouseMap.Clone() : userProfile.mouseMap.Clone(),
            };
            dict[bodyName] = survivorSpecificInputsData;
        }
        if (SurvivorSpecificKeybindsPlugin.keyValuePairs.TryGetValue(userProfile, out SurvivorSpecificKeybindsSave survivorSpecificKeybindsSave))
        {
            survivorSpecificKeybindsSave.keyValuePairs = dict;
        }
        else
        {
            SurvivorSpecificKeybindsPlugin.keyValuePairs[userProfile] = new SurvivorSpecificKeybindsSave
            {
                keyValuePairs = dict
            };
        }
        return survivorSpecificInputsData;
    }
    public static bool TryGetUserProfileSurvivorSpecificInputsData(UserProfile userProfile, string bodyName, out SurvivorSpecificKeybindsData survivorSpecificInputsData)
    {
        if (inputsByBodyNames.TryGetValue(userProfile, out Dictionary<string, SurvivorSpecificKeybindsData> dict) && dict.TryGetValue(bodyName, out survivorSpecificInputsData))
        {
            return true;
        }
        else
        {
            survivorSpecificInputsData = null;
            return false;
        }
    }
    public static FileOutput GetFileOutput(UserProfile userProfile)
    {
        string path = userProfile.filePath.FullName.CleanString() + "/SurvivorSpecificKeybinds.xml";
        return new FileOutput
        {
            fileReference = new FileReference
            {
                path = path,
                fileSystem = userProfile.fileSystem
            },
            requestTime = DateTime.UtcNow,
            contents = Array.Empty<byte>()
        };
    }
    public static void SaveFileOutput(SaveSystem saveSystem, XDocument xDocument, FileOutput fileOutput)
    {
        using (MemoryStream val = new MemoryStream())
        {
            using XmlTextWriter xmlTextWriter = new XmlTextWriter(val, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            xDocument.Save(xmlTextWriter);
            xmlTextWriter.Flush();
            fileOutput.contents = new byte[val.Length];
            val.Seek(0L, SeekOrigin.Begin);
            val.Read(fileOutput.contents, 0, fileOutput.contents.Length);
        }
        saveSystem.EnqueueFileOutput(fileOutput);
    }

    public static string CleanString(this string str)
    {
        return str.Replace(".xml", "").Replace(".cfg", "");
    }
    public static UserProfile GetUserProfile()
    {
        LocalUser localUser = LocalUserManager.GetFirstLocalUser();
        if (localUser == null) return null;
        return localUser.userProfile;
    }
    public static KeyboardMap Clone(this KeyboardMap sourceMap)
    {
        if (sourceMap == null) return null;
        string xml = sourceMap.ToXmlString();
        return ControllerMap.CreateFromXml(ControllerType.Keyboard, xml) as KeyboardMap;
    }
    public static MouseMap Clone(this MouseMap sourceMap)
    {
        if (sourceMap == null) return null;
        string xml = sourceMap.ToXmlString();
        return ControllerMap.CreateFromXml(ControllerType.Mouse, xml) as MouseMap;
    }
    public static JoystickMap Clone(this JoystickMap sourceMap)
    {
        if (sourceMap == null) return null;
        string xml = sourceMap.ToXmlString();
        return ControllerMap.CreateFromXml(ControllerType.Joystick, xml) as JoystickMap;
    }
    public static void AddLanguageToken(string token, string text) => AddLanguageToken(token, text, "en");
    public static void AddLanguageToken(string token, string text, string lang)
    {
        RoR2.Language language = RoR2.Language.GetOrCreateLanguage(lang);
        if (language == null) return;
        if (language.stringsByToken.ContainsKey(token))
        {
            language.stringsByToken[token] = text;
        }
        else
        {
            language.stringsByToken.Add(token, text);
        }
    }
    public static ConfigEntry<T> CreateConfig<T>(string sectionName, string configName, T defaultValue, string description)
    {
        ConfigEntry<T> configEntry = SurvivorSpecificKeybindsPlugin.instance.Config.Bind(sectionName, configName, defaultValue, description);
        return configEntry;
    }
    public static string RemoveUpToAndIncluding(string source, string target, StringComparison comparison = StringComparison.Ordinal)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return source ?? string.Empty;
        int index = source.IndexOf(target, comparison);
        if (index == -1)  return source;
        return source.Substring(index + target.Length);
    }
}