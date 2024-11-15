using System.Xml.Schema;

namespace GuildSaber.Database.Models.Player;

public readonly record struct PlayerHardwareInfo(
    PlayerHardwareInfo.EPlatform Platform,
    PlayerHardwareInfo.EHMD HMD)
{
    public enum EPlatform
    {
        Unk = 0,
        Steam = 1,
        OculusNative = 2,
        OculusPC = 3
    }

    public enum EHMD
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
        Megane1 = 71
    }
}