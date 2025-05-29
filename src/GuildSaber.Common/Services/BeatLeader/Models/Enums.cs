namespace GuildSaber.Common.Services.BeatLeader.Models;

/// <summary>
/// Represents the order in which values will be sorted.
/// </summary>
public enum Order
{
    /// <summary>Descending (0)</summary>
    Desc = 0,

    /// <summary>Ascending (1)</summary>
    Asc = 1
}

public enum HMD
{
    Unknown = 0,
    Rift = 1,
    Vive = 2,
    VivePro = 4,
    WMR = 8,
    RiftS = 16,
    Quest = 32,
    Index = 64,
    ViveCosmos = 128,
    Quest2 = 256,
    Quest3 = 512,
    Quest3S = 513,

    PicoNeo3 = 33,
    PicoNeo2 = 34,
    VivePro2 = 35,
    ViveElite = 36,
    Miramar = 37,
    Pimax8K = 38,
    Pimax5K = 39,
    PimaxArtisan = 40,
    HpReverb = 41,
    SamsungWMR = 42,
    QiyuDream = 43,
    Disco = 44,
    LenovoExplorer = 45,
    AcerWMR = 46,
    ViveFocus = 47,
    Arpara = 48,
    DellVisor = 49,
    E3 = 50,
    ViveDvt = 51,
    Glasses20 = 52,
    Hedy = 53,
    Vaporeon = 54,
    Huaweivr = 55,
    AsusWMR = 56,
    CloudXR = 57,
    Vridge = 58,
    Medion = 59,
    PicoNeo4 = 60,
    QuestPro = 61,
    PimaxCrystal = 62,
    E4 = 63,
    Controllable = 65,
    BigScreenBeyond = 66,
    Nolosonic = 67,
    Hypereal = 68,
    Varjoaero = 69,
    PSVR2 = 70,
    Megane1 = 71,
    VarjoXR3 = 72
}

public enum ControllerEnum
{
    Unknown = 0,
    OculusTouch = 1,
    OculusTouch2 = 16,
    Quest2 = 256,
    Vive = 2,

    VivePro = 4,
    WMR = 8,
    Odyssey = 9,
    HpMotion = 10,

    PicoNeo3 = 33,
    PicoNeo2 = 34,
    VivePro2 = 35,
    Miramar = 37,
    Disco = 44,
    QuestPro = 61,
    ViveTracker = 62,
    ViveTracker2 = 63,
    Knuckles = 64,
    Nolo = 65,
    PicoPhoenix = 66,
    Hands = 67,
    ViveTracker3 = 68,
    Pimax = 69,
    Huawei = 70,
    Polaris = 71,
    Tundra = 72,
    Cry = 73,
    E4 = 74,
    Gamepad = 75,
    JoyCon = 76,
    SteamDeck = 77,
    Etee = 78,
    Quest3 = 79,
    ContactGlove = 80,
    PlayStationSense = 81,
    ViveCosmos = 128
}