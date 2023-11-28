namespace Shared.ItemTypes
{
    [System.Serializable]
    public class Weapon : Item {
        public float Damage = 1;
        public float FireRate = 1;
        public float CriticalChance = 1f;

        public Weapon() {
            MaxStack = 1;
            HoldAnimation = Properties.HoldAnimation.knife;
        }
    }
}
