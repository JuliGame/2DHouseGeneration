using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Shared.Properties;

namespace Shared
{
    [System.Serializable]
    public class Item {
        public String ItemPath = "";
        [Nullable]
        public String Description = "";
        public float PickupRadius = 1f;

        public int MaxStack = 16;
        public float Weight = 1f;
        public float MovingSpeed = 1f;
        
        public HoldAnimation HoldAnimation = HoldAnimation.flashlight;
        
        public Audio ItemEquip;
        
        [Nullable]
        public Image EquipedImage;

        public void SaveAsJson(String path) {
            String json = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            File.WriteAllText(path, json);
        }

        public static Item LoadFromJson(String path, Type type) {
            String json = File.ReadAllText(path);
            return (Item)Newtonsoft.Json.JsonConvert.DeserializeObject(json, type);
        }

        public static Item LoadFromJsonTXT(String text, Type type) {
            return (Item)Newtonsoft.Json.JsonConvert.DeserializeObject(text, type);
        }
        
        
        public Item Clone() {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();

            bf.Serialize(ms, this);

            ms.Position = 0;
            object obj = bf.Deserialize(ms);
            ms.Close();

            return obj as Item;
        }
    }
}
