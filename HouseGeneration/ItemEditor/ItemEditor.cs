using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using Shared;
using Shared.Properties;


namespace HouseGeneration.ItemEditor;

using ImGuiNET;

public class ItemEditor {

    ItemList.ItemExtraData _ItemExtraData = null;
    public Item _item;
    public ItemEditor(ItemList.ItemExtraData item) {
        this._ItemExtraData = item;
        this._item = item.item;
    }

    public void Draw(ItemEditorMain itemEditorMain) {
        ImGui.Begin("Item Editor - " + _item.GetHashCode(), ImGuiWindowFlags.AlwaysAutoResize);
        ImGui.Text(_item.ItemPath);
        ImGui.SameLine(ImGui.GetWindowWidth() - 30);
        if (ImGui.Button("X"))
            itemEditorMain.RemoveItemFromEditor(_ItemExtraData);

        var a = PrintPropertyKeys(_item);
        
        foreach (var entry in a) {
            if (entry.Value.Count > 0) {
                ImGui.TextColored(new System.Numerics.Vector4(0, 1, 1, 1), entry.Key);
                foreach (var fieldInfo in entry.Value) {
                    if (fieldInfo.FieldType == typeof(string)) {
                        String value = (String) fieldInfo.GetValue(_item);
                        ImGui.InputText(fieldInfo.Name, ref value, 255);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                            value = ImGui.GetClipboardText();
                        }
                        fieldInfo.SetValue(_item, value);
                    }
                    if (fieldInfo.FieldType == typeof(int)) {
                        int value = (int) fieldInfo.GetValue(_item);
                        ImGui.InputInt(fieldInfo.Name, ref value);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                            if (int.TryParse(ImGui.GetClipboardText(), out int result)) {
                                value = result;
                            }
                        }
                        fieldInfo.SetValue(_item, value);
                    }
                    if (fieldInfo.FieldType == typeof(float)) {
                        float value = (float) fieldInfo.GetValue(_item);
                        ImGui.InputFloat(fieldInfo.Name, ref value);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                            if (float.TryParse(ImGui.GetClipboardText(), out float result)) {
                                value = result;
                            }
                        }
                        fieldInfo.SetValue(_item, value);
                    }
                    if (fieldInfo.FieldType == typeof(bool)) {
                        bool value = (bool) fieldInfo.GetValue(_item);
                        ImGui.Checkbox(fieldInfo.Name, ref value);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                            if (bool.TryParse(ImGui.GetClipboardText(), out bool result)) {
                                value = result;
                            }
                        }
                        fieldInfo.SetValue(_item, value);
                    }
                    if (fieldInfo.FieldType.IsEnum) {
                        int value = (int) fieldInfo.GetValue(_item);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                            if (int.TryParse(ImGui.GetClipboardText(), out int result)) {
                                value = result;
                            }
                        }
                        ImGui.Combo(fieldInfo.Name, ref value, Enum.GetNames(fieldInfo.FieldType), Enum.GetNames(fieldInfo.FieldType).Length);
                        fieldInfo.SetValue(_item, value);
                    }
                    
                    if (fieldInfo.FieldType == typeof(Image)){
                        Image image = (Image) fieldInfo.GetValue(_item);
                        String value;
                        if (image != null) {
                            value = image.Path;
                        } else {
                            value = _ItemExtraData.imgName;
                        }
                        
                        ImGui.InputText(fieldInfo.Name, ref value, 255);
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                            value = ImGui.GetClipboardText();
                        }
                        
                        if (image != null) {
                            Texture2D texture2D = ItemList.LoadTexture(image.Path);
                            if (texture2D != null) {
                                IntPtr id = ItemList.LoadTexture2D(texture2D);
                                ImGui.Image(id, new System.Numerics.Vector2(100, 100));
                                
                                if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                                    value = ImGui.GetClipboardText();
                                }
                            }
                            image.Path = value;
                        } else {
                            fieldInfo.SetValue(_item, new Image() {Path = value});
                        }
                        
                    }


                    if (ImGui.IsItemHovered()) {
                        ImGui.BeginTooltip();
                        ImGui.TextColored(new System.Numerics.Vector4(0, 1, 1, 1), "Type: " + fieldInfo.FieldType.ToString().Split('.').Last());
                        try {
                            ImGui.TextColored(new System.Numerics.Vector4(0, 1, 1, 1), "Rigth click to paste: " + ImGui.GetClipboardText());
                        }
                        catch (Exception) {
                            // ignored
                        }

                        ImGui.EndTooltip();
                    }

                    
                }
            }
        }
        
        var fields = _item.GetType().GetFields();
        foreach (var fieldInfo in fields) {
            
        }

        if (ItemList.ImagesDict.ContainsKey(_ItemExtraData.imgName))
        {
            
            IntPtr id = ItemList.ImagesDict[_ItemExtraData.imgName];
            ImGui.Image(ItemList.Instance.solidBlack, new System.Numerics.Vector2(100, 100));
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 110);
            ImGui.Image(id, new System.Numerics.Vector2(100, 100));
            ImGui.SameLine();
            ImGui.Image(ItemList.Instance.solidWhite, new System.Numerics.Vector2(100, 100));
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 110);
            ImGui.Image(id, new System.Numerics.Vector2(100, 100));
        }


        
        if (ImGui.Button("Cerrar"))
            itemEditorMain.RemoveItemFromEditor(_ItemExtraData, false);
        
        ImGui.SameLine();
        if (ImGui.Button("Auto Fix")) {
            if (_ItemExtraData.imgName != _ItemExtraData.item.ItemPath) {
                _ItemExtraData.item.ItemPath = _ItemExtraData.imgName;
            }
        }
        
        ImGui.SameLine();
        if (ImGui.Button("Save"))
            itemEditorMain.Save(_ItemExtraData);
        
        ImGui.SameLine(ImGui.GetWindowWidth() - 90);
            
            
        if (ImGui.Button("Save exit"))
            itemEditorMain.RemoveItemFromEditor(_ItemExtraData);
        
        ImGui.End();
    }
    
    public static Dictionary<string, List<FieldInfo>> PrintPropertyKeys<T>(T obj) where T : class
    {
        var objectType = obj.GetType();
        List<Type> types = new List<Type>();
        while (objectType != null) {
            types.Add(objectType);
            objectType = objectType.BaseType;
        }
        types.Reverse();
        
        var fieldsDict = new Dictionary<string, List<FieldInfo>>();
        foreach (var type in types) {
            // Use BindingFlags to get only the fields declared in the current class, not the inherited ones
            var field = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(field => field).ToList();
            fieldsDict.Add(type.Name, field);
        }
        return fieldsDict;
    }
}
