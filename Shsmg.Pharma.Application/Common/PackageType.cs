namespace Shsmg.Pharma.Application.Common;

public enum PackageType
{
    // Pharma
    Tablet,
    Capsule,
    Strip,
    Blister,
    Bottle,
    Vial,
    Ampoule,
    Sachet,
    Tube,
    Syringe,

    // General retail
    Unit,
    Piece,
    Pack,
    Box,
    Carton,
    Case,
    Bundle,
    Set,
    Kit,

    // Storage / transport
    Bag,
    Pouch,
    Crate,
    Pallet,
    Drum,
    Barrel,
    Container,
    Tank,

    // Food & beverage
    Can,
    Jar,
    Cup,
    Tray,
    Wrapper,

    // Industrial / hardware
    Roll,
    Coil,
    Reel,
    Sheet,

    // Electronics
    Cartridge,

    // Misc
    Pair,
    Dozen,
    Packet
}