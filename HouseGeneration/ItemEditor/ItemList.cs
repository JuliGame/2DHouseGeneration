using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;

namespace HouseGeneration.ItemEditor;

using ImGuiNET;

public class ItemList
{
    private String path = "/home/julian/ZEscape/Assets/Resources/Items";
    
    private ItemEditorMain ItemEditorMain;

    public class ItemExtraData {
        public IntPtr id;
        public Item item;
        public String imgName;
        public string fullPath;
        public String categories;
    }
    
    List<ItemExtraData> _images = new List<ItemExtraData>();
    IntPtr solidGreen;
    IntPtr solidYellow;
    IntPtr solidBlue;
    IntPtr solidRed;
    FileSystemWatcher watcher = new FileSystemWatcher();
    
    public static Dictionary<string, IntPtr> ImagesDict = new Dictionary<string, IntPtr>();
    public ItemList(ItemEditorMain itemEditorMain) {
        this.ItemEditorMain = itemEditorMain;
        
        Texture2D solidGreen = new Texture2D(ItemEditorMain.GraphicsDevice, 1, 1);
        solidGreen.SetData(new[] { Color.Green });
        this.solidGreen = ItemEditorMain.GuiRenderer.BindTexture(solidGreen);
        
        Texture2D solidYellow = new Texture2D(ItemEditorMain.GraphicsDevice, 1, 1);
        solidYellow.SetData(new[] { Color.Yellow });
        this.solidYellow = ItemEditorMain.GuiRenderer.BindTexture(solidYellow);
        
        Texture2D solidBlue = new Texture2D(ItemEditorMain.GraphicsDevice, 1, 1);
        solidBlue.SetData(new[] { Color.Blue });
        this.solidBlue = ItemEditorMain.GuiRenderer.BindTexture(solidBlue);
        
        Texture2D solidRed = new Texture2D(ItemEditorMain.GraphicsDevice, 1, 1);
        solidRed.SetData(new[] { Color.Red });
        this.solidRed = ItemEditorMain.GuiRenderer.BindTexture(solidRed);
        
        watcher.Path = path;
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
        watcher.Changed += OnChanged;
        watcher.Created += OnChanged; 
        watcher.Deleted += OnChanged;

        watcher.EnableRaisingEvents = true;
        debounceTimer.Elapsed += (s, e) => Init();
    }
    
    private static Timer debounceTimer = new Timer(50) { AutoReset = false };
    private void OnChanged(object source, FileSystemEventArgs e) {
        Console.Out.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
        
        debounceTimer.Stop();
        debounceTimer.Start();

        Console.WriteLine($"File: {e.FullPath} {e.ChangeType}");
    }
    
    void ProcessDirectory(string targetDirectory, List<string> images, List<string> dotItems)
    {
        // Procesa los archivos en este directorio.
        foreach (string file in System.IO.Directory.GetFiles(targetDirectory))
        {
            if (file.EndsWith(".png")) {
                // get path after main path
                images.Add(file.Substring(path.Length + 1, file.Length - path.Length - 5));
            }
            else if (file.EndsWith(".txt")) {
                dotItems.Add(file.Substring(path.Length + 1, file.Length - path.Length - 5));
            }
        }

        // Recursivamente procesa cada subdirectorio.
        string[] subdirectoryEntries = System.IO.Directory.GetDirectories(targetDirectory);
        foreach(string subdirectory in subdirectoryEntries)
            ProcessDirectory(subdirectory, images, dotItems); 
    }
    public void Init() {
        _images.Clear();
        ImagesDict.Clear();
        List<String> images = new List<string>();
        List<String> dotItems = new List<string>();
        
        ProcessDirectory(path, images, dotItems);
        
        foreach (var itemFileName in images) {
            Texture2D texture2D = LoadTexture(path + "/" + itemFileName + ".png");
            IntPtr id =  ItemEditorMain.GuiRenderer.BindTexture(texture2D);
            
            ItemExtraData itemExtraData = new ItemExtraData();
            itemExtraData.id = id;
            itemExtraData.imgName = itemFileName;
            String fullPath = path + "/" + itemFileName;
            itemExtraData.fullPath = fullPath;
            
            if (itemFileName.Contains("/")) {
                itemExtraData.categories = fullPath.Remove(0, path.Length + 1).Remove(itemFileName.LastIndexOf("/"), itemFileName.Length - itemFileName.LastIndexOf("/"));
                Console.Out.WriteLine("itemFileName = {0}", itemFileName);
            } else {
                itemExtraData.categories = "";
            }
            
            if (dotItems.Contains(itemFileName))
            {
                itemExtraData.item = GetItem(itemExtraData);
            }
            
            this._images.Add(itemExtraData);
            ImagesDict.Add(itemFileName, id);
        }
    }

    public Item GetItem(ItemExtraData itemExtraData) {
        bool exists = File.Exists(itemExtraData.fullPath + ".txt");
        
        String fullPath = itemExtraData.fullPath;
        
        Item item = null;
        String[] categories = fullPath.Remove(0, path.Length + 1).Split("/");
        if (categories.Length != 0)
        {
            var assembly = System.Reflection.Assembly.Load("Shared");
            String namespacePrefix = "Shared.ItemTypes";
            
            // print all assembly classes
            // foreach (var type in assembly.GetTypes()) {
            //     Console.WriteLine(type.FullName);
            // }
            for (int i = categories.Length; i > 0; i--) {
                String className = string.Join(".", categories.ToList().Take(i));
                if (i != categories.Length && className.EndsWith("s"))
                    className = className.Remove(className.Length - 1, 1);
                
                string fullTypeName = namespacePrefix + "." + className;
                try {
                    Type itemType = assembly.GetType(fullTypeName, false, true);
                    if (itemType != null) {
                        // Si encontramos la clase, instanciamos un objeto de ese tipo y terminamos
                        if (!exists)
                            item = (Item)Activator.CreateInstance(itemType);
                        else
                            item = Item.LoadFromJson(itemExtraData.fullPath + ".txt", itemType);
                        
                        // System.Console.Out.WriteLine("Found class: " + fullTypeName + "!!!!!!!");
                        break;
                    }
                    else {
                        // System.Console.WriteLine("No class found for: " + fullTypeName);
                        continue;
                    }
                }
                catch (Exception e) {
                    throw e;
                }
            }
        }
        
        if (item == null) {
            if (!exists)
                item = new Item();
            else
                item = Item.LoadFromJson(path + "/" + itemExtraData.imgName + ".txt", typeof(Item));
        }

        return item;
    }

    public void Draw() {
        ImGui.Begin("Item List");
        ImGui.Text("Items");
        if (ImGui.Button("Reload"))
            Init();
        
        Dictionary<String, List<ItemExtraData>> categories = new Dictionary<String, List<ItemExtraData>>();
        foreach (var imgData in _images) {
            if (!categories.ContainsKey(imgData.categories)) {
                categories.Add(imgData.categories, new List<ItemExtraData>());
            }
            categories[imgData.categories].Add(imgData);
        }
        ImGui.BeginChild("Scrolling", new System.Numerics.Vector2(0), true);
        foreach (var VARIABLE in categories) {
            String category = VARIABLE.Key;
            if (category == "")
                category = "No Category";
            ImGui.Text(category);

            int perRow = (int) (ImGui.GetWindowWidth() / 100) - 1;
            if (perRow < 1)
                perRow = 1;
        
            int i = 0;
        
            foreach (var imgData in VARIABLE.Value) {
                if (i % perRow != 0)
                    ImGui.SameLine();
            
                ImGui.BeginGroup();
                
                List<String> errors = IsComplete(imgData);
                
                if (imgData.imgName.Contains("/"))
                    ImGui.Text(imgData.imgName.Substring(imgData.imgName.LastIndexOf("/") + 1));
                else
                    ImGui.Text(imgData.item?.ItemPath ?? imgData.imgName);
            
                if (imgData.item != null && ItemEditorMain.CurrentItems.ContainsKey(imgData.item))
                    ImGui.Image(solidBlue, new System.Numerics.Vector2(100, 100));
                else if (imgData.item != null)
                    if (errors.Count != 0)
                        ImGui.Image(solidRed, new System.Numerics.Vector2(100, 100));
                    else
                        ImGui.Image(solidGreen, new System.Numerics.Vector2(100, 100));
                else
                    ImGui.Image(solidYellow, new System.Numerics.Vector2(100, 100));
            
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() - 110);
                ImGui.Image(imgData.id, new System.Numerics.Vector2(100, 100));
                ImGui.EndGroup();
                if (ImGui.IsItemHovered()) {
                    ImGui.BeginTooltip();
                
                    if (imgData.item != null)
                        ImGui.Text("Item Name: " + imgData.item.ItemPath);
                
                    ImGui.Text("Texture Name: " + imgData.imgName);
                    
                    if (errors.Count > 1)
                        ImGui.Text("");
                    foreach (var error in errors) 
                        ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), error);
                    
                    ImGui.EndTooltip();
                }

                if (ImGui.IsItemClicked()) {
                    if (imgData.item != null && ItemEditorMain.CurrentItems.ContainsKey(imgData.item)) {
                        ItemEditorMain.RemoveItemFromEditor(imgData);
                    }
                    else {
                        if (imgData.item == null) {
                            imgData.item = GetItem(imgData);
                            imgData.item.ItemPath = imgData.imgName;
                        }
                        ItemEditorMain.AddItemToEditor(imgData);
                    }
                }
                i++;
            }

        }
        ImGui.EndChild();
        ImGui.End();
    }

    private List<string> IsComplete(ItemExtraData imgData) {
        List<string> errors = new List<string>();
        if (imgData.item == null) {
            errors.Add("[Error] Item is null");
            return errors;
        }
        foreach (var field in imgData.item.GetType().GetFields()) {
            if (field.GetValue(imgData.item) == null) {
                errors.Add("[Error] missing " + field.Name);
                continue;
            }
            // if field is a string, check that is not empty
            if (field.FieldType == typeof(String) && (String)field.GetValue(imgData.item) == "")
                errors.Add("[Error] missing " + field.Name);
        }

        if (imgData.imgName != imgData.item.ItemPath) {
            errors.Add("[Error] ItemPath (text) is not equal to real item path");
            errors.Add("Is should be: " + imgData.imgName);
        }
        
        return errors;
    }

    Texture2D LoadTexture(string path)
    {
        using (FileStream fileStream = new FileStream(path, FileMode.Open))
        {
            return Texture2D.FromStream(ItemEditorMain.GraphicsDevice, fileStream);
        }
    }
}
