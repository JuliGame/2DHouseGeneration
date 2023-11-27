

namespace Shared.ItemTypes.Weapons
{
    public class MeleGun : Weapon {
        public MeleGun() {
            MaxStack = 1;
            HoldAnimation = Properties.HoldAnimation.handgun;
        }
    }
}
