using HarmonyLib;

using ResoniteModLoader;

namespace MockVisemes;
//More info on creating mods can be found https://github.com/resonite-modding-group/ResoniteModLoader/wiki/Creating-Mods
public class MockVisemes : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.0.0"; //Changing the version here updates it in all locations needed
	public override string Name => "MockVisemes";
	public override string Author => "Baplar";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/Baplar/ResoniteMockVisemes/";

	private static ModConfiguration Config;

	public override void OnEngineInit() {
		Config = GetConfiguration();
		Config.Save(true);

		Harmony harmony = new("fr.baplar.MockVisemes");
		harmony.PatchAll();
	}

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<bool> EnabledKey = new("enabled", "Enable mock viseme generation", () => true);

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<bool> ForceKey = new("force", "(Requires session rejoin) Force mock viseme generation even if OVRLipSync is available, e.g. on Windows", () => false);

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<float> MinLerpSpeedKey = new(
		"minLerpSpeed",
		"Minimum lerp speed when the smoothing parameter of the VisemeAnalyzer is close to 1",
		computeDefault: () => 4,
		valueValidator: (val) => val >= 0 && val <= MaxLerpSpeed
		);

	[AutoRegisterConfigKey]
	public static readonly ModConfigurationKey<float> MaxLerpSpeedKey = new(
		"maxLerpSpeed",
		"Maximum lerp speed when the smoothing parameter of the VisemeAnalyzer is close to 0",
		computeDefault: () => 20,
		valueValidator: (val) => val >= MinLerpSpeed && val <= 30
		);

	public static bool Enabled => Config.GetValue(EnabledKey);

	public static bool Force => Config.GetValue(ForceKey);

	public static float MinLerpSpeed => Config.GetValue(MinLerpSpeedKey);

	public static float MaxLerpSpeed => Config.GetValue(MaxLerpSpeedKey);
}
