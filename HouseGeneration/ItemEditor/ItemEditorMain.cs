using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.ImGuiNet;
using Shared;

namespace HouseGeneration.ItemEditor;

public class ItemEditorMain : Game
{
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    public ImGuiRenderer GuiRenderer;
    public List<ItemEditor>  ItemEditors = new List<ItemEditor>();

    public ItemEditorMain() {
        graphics = new GraphicsDeviceManager(this);
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
            item.item.SaveAsJson(item.fullPath +  ".txt");
    }
}
