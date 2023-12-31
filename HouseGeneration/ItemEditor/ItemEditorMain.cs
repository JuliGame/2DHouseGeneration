using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.ImGuiNet;
using Shared;

namespace HouseGeneration.ItemEditor;

public class ItemEditorMain : Game
{
    public static GraphicsDeviceManager Graphics;
    private SpriteBatch spriteBatch;
    public static ImGuiRenderer GuiRenderer;
    public List<ItemEditor>  ItemEditors = new List<ItemEditor>();
    public ItemEditorMain() {
        Graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        // Inicializa MonoGame.ImGuiNet
        
        GuiRenderer = new ImGuiRenderer(this);
        // set to fullscreen
        
        itemList = new ItemList(this);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        GuiRenderer.RebuildFontAtlas();
        
        itemList.Init();
    }

    protected override void Update(GameTime gameTime) {
        // Cierra la aplicación cuando se presiona la tecla Escape
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        

        base.Update(gameTime);
    }

    ItemList itemList;
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        base.Draw(gameTime);

        GuiRenderer.BeginLayout(gameTime);
        itemList.Draw();
        foreach (var itemEditor in ItemEditors.ToList()) {
            itemEditor.Draw(this);
        }
        
        GuiRenderer.EndLayout();
    }
    
    public Dictionary<Item, ItemEditor> CurrentItems = new Dictionary<Item, ItemEditor>();
    public void AddItemToEditor(ItemList.ItemExtraData item) {
        if (CurrentItems.ContainsKey(item.item))
            return;
        
        ItemEditor itemEditor = new ItemEditor(item);
        ItemEditors.Add(itemEditor);
        CurrentItems.Add(item.item, itemEditor);
    }
    
    public void RemoveItemFromEditor(ItemList.ItemExtraData item, bool save = true) {
        if (!CurrentItems.ContainsKey(item.item))
            return;
        
        ItemEditors.Remove(CurrentItems[item.item]);
        CurrentItems.Remove(item.item);
        
        if (save)
            Save(item);
    }

    public void Save(ItemList.ItemExtraData item) {
        item.item.SaveAsJson(item.fullPath +  ".txt");
    }
    
    public void PlaySound(string filePath) {
        // Crear un proceso para ejecutar el comando mpg123
        Thread t = new Thread(() =>
        {
            Process process = new Process();
            process.StartInfo.FileName = "mpg123";
            process.StartInfo.Arguments = filePath;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            // Iniciar el proceso y esperar a que termine
            process.Start();
            process.WaitForExit();
        });
        t.Start();
    }
}
