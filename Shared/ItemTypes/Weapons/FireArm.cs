

namespace Shared.ItemTypes.Weapons
{
    [System.Serializable]
    public class FireArm : Weapon {
        public float Damage = 1;
        public float FireRate = 1;
        public float CriticalChance = 1f;
        
        public int MaxAmmo = 7;
        public int CurrentAmmo = 7;
        public float ReloadTime = 1;
        public int ReloadAmount = 7;
    
        public float Recoil = 1;
        public float Precision = 1;
    
        public float Range = 10;
        
        public FireArm() {
            MaxStack = 1;
            HoldAnimation = Properties.HoldAnimation.handgun;
        }
    }
}
