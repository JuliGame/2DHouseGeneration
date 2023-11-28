

namespace Shared.ItemTypes.Weapons
{
    [System.Serializable]
    public class MeleGun : Weapon {
        public MeleGun() {
            MaxStack = 1;
            HoldAnimation = Properties.HoldAnimation.handgun;
        }
    }
}
