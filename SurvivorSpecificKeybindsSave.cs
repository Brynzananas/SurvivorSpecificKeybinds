using Rewired;
using SurvivorSpecificKeybinds;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using UnityEngine.UIElements;

namespace SurvivorSpecificKeybinds;
public class SurvivorSpecificKeybindsSave
{
    public Dictionary<string, SurvivorSpecificKeybindsData> keyValuePairs = [];
    public XDocument ToXml()
    {
        List<object> list = [];
        XElement xelement = new XElement("SurvivorSpecificKeybinds");
        list.Add(xelement);
        if (keyValuePairs != null)
            foreach (var pair in keyValuePairs)
            {
                XElement xelement1 = new XElement("SurvivorSpecificKeybindsSlot");
                xelement.Add(xelement1);
                XElement xelement2 = new XElement("BodyName", pair.Key);
                xelement1.Add(xelement2);
                SurvivorSpecificKeybindsData survivorSpecificKeybindsData = pair.Value;
                XElement xelement3 = new XElement("KeyboardMap", survivorSpecificKeybindsData.keyboardMap.ToXmlString());
                xelement1.Add(xelement3);
                XElement xelement4 = new XElement("MouseMap", survivorSpecificKeybindsData.mouseMap.ToXmlString());
                xelement1.Add(xelement4);
                XElement xelement5 = new XElement("JoystickMap", survivorSpecificKeybindsData.joystickMap.ToXmlString());
                xelement1.Add(xelement5);
            }
        return new XDocument(list.ToArray());
    }
    public static SurvivorSpecificKeybindsSave FromXml(XDocument xDocument)
    {
        try
        {
            SurvivorSpecificKeybindsSave bomebrWispStageRules = new SurvivorSpecificKeybindsSave();
            XElement root = xDocument.Root;
            if (root == null) return bomebrWispStageRules;
            foreach (XElement xElement in root.Elements("SurvivorSpecificKeybindsSlot"))
            {
                XElement xElement1 = xElement.Element("BodyName");
                string bodyName = null;
                SurvivorSpecificKeybindsData survivorSpecificKeybindsData = new SurvivorSpecificKeybindsData();
                if (xElement1 != null) bodyName = xElement1.Value;
                survivorSpecificKeybindsData.bodyName = bodyName;
                XElement xElement2 = xElement.Element("KeyboardMap");
                string keyboardMapString = null;
                if (xElement2 != null) keyboardMapString = xElement2.Value;
                survivorSpecificKeybindsData.keyboardMap = ControllerMap.CreateFromXml(ControllerType.Keyboard, keyboardMapString) as KeyboardMap;
                XElement xElement3 = xElement.Element("MouseMap");
                string mouseMapString = null;
                if (xElement3 != null) mouseMapString = xElement3.Value;
                survivorSpecificKeybindsData.mouseMap = ControllerMap.CreateFromXml(ControllerType.Mouse, mouseMapString) as MouseMap;
                XElement xElement4 = xElement.Element("JoystickMap");
                string joystickMapString = null;
                if (xElement4 != null) joystickMapString = xElement4.Value;
                survivorSpecificKeybindsData.joystickMap = ControllerMap.CreateFromXml(ControllerType.Joystick, joystickMapString) as JoystickMap;
                bomebrWispStageRules.keyValuePairs.Add(bodyName, survivorSpecificKeybindsData);
            }
            return bomebrWispStageRules;
        }
        catch (Exception e)
        {
            SurvivorSpecificKeybindsPlugin.instance.Logger.LogError(e);
            return null;
        }
    }
}
