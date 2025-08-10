using HarmonyLib;
using FrooxEngine;
using Elements.Core;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace MockVisemes;

public static class VisemeContextMap {
    private static uint LastComputerContext = 1000;
    private static Dictionary<uint, RNGLipSyncComputer> VisemeComputers = [];

    public static uint CreateVisemeComputer() {
        uint context = LastComputerContext++;
        VisemeComputers.Add(context, new RNGLipSyncComputer());
        return context;
    }

    public static RNGLipSyncComputer Get(uint context) => VisemeComputers[context];
    public static bool Delete(uint context) => VisemeComputers.Remove(context);
}

[HarmonyPatch(typeof(VisemeAnalyzer), "OnAwake")]
public static class VisemeAnalyzer_OnAwake_Patcher {
    public static void Postfix(ref OVRLipSyncContext ___analysisContext, Sync<float> ___Smoothing) {
        if (___analysisContext != null && !MockVisemes.Force) {
            return;
        }

        UniLog.Warning("Initializing stub RNG-based viseme analyzer");
        ___analysisContext?.Dispose();
        OVRLipSyncContext context = new(null);
        context.Update(___Smoothing);
        ___Smoothing.OnValueChange += (smoothing) => context.Update(smoothing);
        ___analysisContext = context;
    }
}

[HarmonyPatch(typeof(OVRLipSyncContext), MethodType.Constructor, [typeof(OVRLipSyncInterface)])]
public static class OVRLipSyncContext_Constructor_Patcher {
    public static bool Prefix(OVRLipSyncInterface ovrLipSync, ref OVRLipSyncContext __instance) {
        if (ovrLipSync != null) {
            // Actually a real instanciation, let it do its job
            return true;
        }

        uint context = VisemeContextMap.CreateVisemeComputer();
        AccessTools.Field(typeof(OVRLipSyncContext), "context").SetValue(__instance, context);
        return false;
    }
}

[HarmonyPatch(typeof(OVRLipSyncContext), "Update")]
public static class OVRLipSyncContext_Update_Patcher {
    public static bool Prefix(float smoothing, OVRLipSyncInterface ___ovrLipSync, uint ___context) {
        if (___ovrLipSync != null) {
            // Actually a real instanciation, let it do its job
            return true;
        }

        RNGLipSyncComputer computer = VisemeContextMap.Get(___context);
        if (computer != null) {
            computer.Smoothing = smoothing;
        }

        return false;
    }
}

[HarmonyPatch(typeof(OVRLipSyncContext), "Dispose")]
public static class OVRLipSyncContext_Dispose_Patcher {
    public static bool Prefix(OVRLipSyncInterface ___ovrLipSync, uint ___context) {
        if (___ovrLipSync != null) {
            // Actually a real instanciation, let it do its job
            return true;
        }

        VisemeContextMap.Delete(___context);
        return false;
    }
}

[HarmonyPatch(typeof(OVRLipSyncContext), "Analyze")]
public static class OVRLipSyncContext_Analyze_Patcher {
    public static bool Prefix(float[] audioData, float[] analysis, Action onDone, OVRLipSyncInterface ___ovrLipSync, uint ___context) {
        if (___ovrLipSync != null) {
            // Actually a real instanciation, let it do its job
            return true;
        }

        Task.Run(() => MockAnalyze(audioData, analysis, ___context)).ContinueWith((_) => onDone());
        return false;
    }

    private static void MockAnalyze(float[] audioData, float[] analysis, uint context) {
        RNGLipSyncComputer computer = VisemeContextMap.Get(context);
        if (computer == null) {
            return;
        }

        if (!MockVisemes.Enabled) {
            computer.ResetContext();
            Array.Clear(analysis);
            return;
        }

        OVRLipSyncInterface.Frame frame = computer.ProcessFrame(audioData);
        frame.Visemes.CopyTo(analysis, 0);
    }
}