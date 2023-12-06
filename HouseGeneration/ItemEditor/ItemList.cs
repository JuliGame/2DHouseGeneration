using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shared;
using Shared.Properties;

namespace HouseGeneration.ItemEditor;

using ImGuiNET;

public class ItemList
{
    private static String path = "/home/julian/ZEscape/Assets/Resources/Items";
    
    private ItemEditorMain ItemEditorMain;

    public class ItemExtraData {
        public IntPtr id;
        public Item item;
        public String imgName;
        public string fullPath;
        public String categories;
    }
    
    List<ItemExtraData> _images = new List<ItemExtraData>();
    public IntPtr solidGreen;
    public IntPtr solidYellow;
    public IntPtr solidBlue;
    public IntPtr solidRed;
    public IntPtr solidBlack;
    public IntPtr solidWhite;
    FileSystemWatcher watcher = new FileSystemWatcher();
    
    public static Dictionary<string, IntPtr> ImagesDict = new Dictionary<string, IntPtr>();
    public static ItemList Instance;
    public ItemList(ItemEditorMain itemEditorMain) {
        this.ItemEditorMain = itemEditorMain;
        Instance = this;
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
        
        Texture2D solidBlack = new Texture2D(ItemEditorMain.GraphicsDevice, 1, 1);
        solidBlack.SetData(new[] { Color.Black });
        this.solidBlack = ItemEditorMain.GuiRenderer.BindTexture(solidBlack);
        
        Texture2D solidWhite = new Texture2D(ItemEditorMain.GraphicsDevice, 1, 1);
        solidWhite.SetData(new[] { Color.White });
        this.solidWhite = ItemEditorMain.GuiRenderer.BindTexture(solidWhite);
        
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
        
        if (!Directory.Exists(path))
            return;
        
        ProcessDirectory(path, images, dotItems);
        
        foreach (var itemFileName in images) {
            Texture2D texture2D = LoadTexture(itemFileName);
            IntPtr id =  ItemEditorMain.GuiRenderer.BindTexture(texture2D);
            
            ItemExtraData itemExtraData = new ItemExtraData();
            itemExtraData.id = id;
            itemExtraData.imgName = itemFileName;
            String fullPath = path + "/" + itemFileName;
            itemExtraData.fullPath = fullPath;
            
            if (itemFileName.Contains("/")) {
                itemExtraData.categories = fullPath.Remove(0, path.Length + 1).Remove(itemFileName.LastIndexOf("/"), itemFileName.Length - itemFileName.LastIndexOf("/"));
            } else {
                itemExtraData.categories = "";
            }
            
            if (dotItems.Contains(itemFileName))
            {
                itemExtraData.item = GetItem(itemExtraData);
            }
            
            _images.Add(itemExtraData);
            ImagesDict.Add(itemFileName, id);
        }
        
        // iterate through all the fields of the item to see if any is of type Image
        List<string> imagesUsedByItems = new List<string>();
        foreach (var itemExtraData in _images) {
            if (itemExtraData.item == null)
                continue;
            
            foreach (var fieldInfo in itemExtraData.item.GetType().GetFields()) {
                if (fieldInfo.FieldType == typeof(Image)) {
                    Image image = (Image) fieldInfo.GetValue(itemExtraData.item);
                    if (image != null) {
                        if (image.Path != itemExtraData.item.ItemPath)
                            imagesUsedByItems.Add(image.Path);
                    }
                }
            }
        }
        
        foreach (var itemFileName in imagesUsedByItems) {
            foreach (var itemExtraData in _images.ToList()) {
                if (itemExtraData.imgName != itemFileName) {
                    continue;
                }
                _images.Remove(itemExtraData);
            }
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
        
        ImGui.InputText("Path: ", ref path, 255);
        
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
                
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right)){
                    ImGui.SetClipboardText(imgData.imgName);
                }
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && ImGui.GetIO().KeyCtrl){
                    File.Delete(imgData.fullPath + ".txt");
                    Init();
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
            bool missing = false;
            if (field.GetValue(imgData.item) == null) {
                missing = true;
                continue;
            }
            // if field is a string, check that is not empty
            if (field.FieldType == typeof(String) && (String)field.GetValue(imgData.item) == "")
                missing = true;
            
            var attrs = field.GetCustomAttributes(true);
            bool hasNullable = false;
            foreach (var attr in attrs) {
                if (attr is NullableAttribute)
                    hasNullable = true;
            }
            if (hasNullable)
                continue;
            
            if (missing)
                errors.Add("[Error] missing " + field.Name);
        }

        if (imgData.imgName != imgData.item.ItemPath) {
            errors.Add("[Error] ItemPath (text) is not equal to real item path");
            errors.Add("Is should be: " + imgData.imgName);
        }
        
        return errors;
    }

    private static Dictionary<string, Texture2D> hashedTextures2D = new Dictionary<string, Texture2D>();
    public static Texture2D LoadTexture(string imgPath) {
        imgPath = path + "/" + imgPath + ".png";
        try {
            if (hashedTextures2D.ContainsKey(imgPath))
                return hashedTextures2D[imgPath];
            
            using (FileStream fileStream = new FileStream(imgPath, FileMode.Open)) {
                Texture2D t2 = Texture2D.FromStream(ItemEditorMain.Graphics.GraphicsDevice, fileStream);
                hashedTextures2D.Add(imgPath, t2);
                return t2;
            }
        }
        catch (Exception e) {
            return null;
        }
    }
    
    private static Dictionary<Texture2D, IntPtr> hashedTextures = new Dictionary<Texture2D, IntPtr>();
    public static IntPtr LoadTexture2D(Texture2D texture2D) {
        if (hashedTextures.ContainsKey(texture2D))
            return hashedTextures[texture2D];
        
        IntPtr id = ItemEditorMain.GuiRenderer.BindTexture(texture2D);
        hashedTextures.Add(texture2D, id);
        return id;
    }
}
