namespace Shared.ItemTypes
{
    [System.Serializable]
    public class Weapon : Item {
        public float MeleDamage = 1;
        public float MeleRate = 1;
        public float MelePush = 1;
        public float MeleCriticalChance = 1f;

        public Weapon() {
            MaxStack = 1;
            HoldAnimation = Properties.HoldAnimation.knife;
        }
    }
}
