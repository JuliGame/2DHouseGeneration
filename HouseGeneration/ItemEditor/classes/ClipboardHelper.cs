using System;
using System.Reflection;
using Accord.IO;
using ImGuiNET;
using Shared;
using Shared.Properties;

namespace HouseGeneration.ItemEditor.classes;

public class ClipboardHelper
{
    // [Serializable]
    public class ClipboardHelperData {
        public object fieldVal;
        public Type fieldType;
        
        public ClipboardHelperData() { }
        public ClipboardHelperData(object fieldVal, Type fieldType) {
            this.fieldVal = fieldVal;
            this.fieldType = fieldType;
        }
    }
    
    public static object Instance { get; set; } = null;
    
    public static void HandleRightClick(FieldInfo fieldInfo, Object o) {
        if (ImGui.IsKeyDown(ImGui.GetKeyIndex(ImGuiKey.ModShift)) || Instance == null) {
            copyToClipboard(fieldInfo, o);
            return;
        }
        
        
        if (Instance is ClipboardHelperData clipboardHelperData) {
            if (fieldInfo == null) {
                if (Instance is ClipboardHelperData data) {
                    Console.Out.WriteLine("paste to object");
                }
                return;
            }
            else if (fieldInfo.FieldType == clipboardHelperData.fieldType) {
                fieldInfo.SetValue(o, clipboardHelperData.fieldVal);
                return;
            }
            
            copyToClipboard(fieldInfo, o);
        }
        else {
            string val = "";
            if (Instance is Audio audio)
                val = audio.Path;
            else if (Instance is Image image)
                val = image.Path;
            else if (Instance is Item item)
                val = item.ItemPath;
            else {
                Console.Out.WriteLine("Type not implemented " + Instance.GetType());
                return;
            }

            if (fieldInfo == null) {
                Console.Out.WriteLine("fieldInfo is null");
                return;
            }

            if (fieldInfo.FieldType == typeof(string)) {
                fieldInfo.SetValue(o, val);
            }
            else if (fieldInfo.FieldType == typeof(Audio)) {
                fieldInfo.SetValue(o, new Audio() {Path = val});
            }
            else if (fieldInfo.FieldType == typeof(Image)) {
                fieldInfo.SetValue(o, new Image() {Path = val});
            }
            else {
                Console.Out.WriteLine("not implemented");
            }
        }
    }
    
    private static void copyToClipboard(FieldInfo fieldInfo, Object o) {
        if (fieldInfo == null) {
            if (o is ClipboardHelperData data) {
                Instance = new ClipboardHelperData(data.fieldVal, data.fieldType);
            }
            else {
                Instance = o.DeepClone();   
            }
        }
        else
            Instance = new ClipboardHelperData() {
                fieldVal = fieldInfo.GetValue(o).DeepClone(),
                fieldType = fieldInfo.FieldType
            };
        Console.Out.WriteLine("copied to clipboard " + Instance);
    }
    
    public static void PasteToObject(object obj) {
        
    }
}

