using System;
using System.IO;
using Shared.Properties;

namespace Shared
{
    public class Item {
        public String ItemPath = "";
        public String Description = "";
        public float PickupRadius = 1f;

        public int MaxStack = 16;
        public float Weight = 1f;
        public float MovingSpeed = 1f;
        
        public HoldAnimation HoldAnimation = HoldAnimation.flashlight;
        
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
    }
}
