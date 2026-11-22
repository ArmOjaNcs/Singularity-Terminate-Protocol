namespace ECS.WeaponComponents
{
    public enum WeaponType
    {
        MeleeDefault = 0,
        Axe = 1,
        ElectricRifle = 2,
        GrenadeLauncher = 3,
        Revolver = 4,
        AssaultRifle = 5,
        Ripper = 6,
        Sword = 7,
        Turret = 8,
        ElectricTurret = 9,
        MeleeSmall = 10,
        MeleeLarge = 11,
        Shotgun = 12,
        FrostRifle = 13,
        RocketLauncher = 14,
        Shuriken = 15,
        BladeGuard = 16,
        Volt = 17,
        OmegaRipper = 18,
        TacticalComplex = 19,
        Armageddon6 = 20,
    }

    public static class WeaponTypeExtensions
    {
        public static bool UsesRipperAttack(this WeaponType type)
        {
            return type == WeaponType.Ripper || type == WeaponType.OmegaRipper;
        }

        public static bool UsesSwordAttack(this WeaponType type)
        {
            return type == WeaponType.Sword || type == WeaponType.Shuriken;
        }
    }
}
