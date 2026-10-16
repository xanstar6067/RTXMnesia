using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.ScriptApi
{
    // General engine functions: debug, random, strings, timers, variables, map (LuxScriptHandler.cpp).
    public abstract partial class HplScript
    {
        string _lastDebugMessage;

        protected void Print(string asString) => Debug.Log("[Script] " + asString);

        /// <summary>Shown by the debug handler in HPL2 (debug mode); here in the console if the map allows it.</summary>
        protected void AddDebugMessage(string asString, bool abCheckForDuplicates)
        {
            if (!Map.logDebugMessages) return;
            if (abCheckForDuplicates && asString == _lastDebugMessage) return;
            _lastDebugMessage = asString;
            Debug.Log($"[Script debug {Map.GameTime:0.00}] {asString}");
        }

        protected void ProgLog(string asLevel, string asMessage) => Debug.Log($"[Script progress {asLevel}] {asMessage}");

        protected bool ScriptDebugOn() => Map.scriptDebug;

        protected float RandFloat(float afMin, float afMax) => HplMath.RandRectf(afMin, afMax);

        protected int RandInt(int alMin, int alMax) => HplMath.RandRectl(alMin, alMax);

        protected bool StringContains(string asString, string asSubString) =>
            asString != null && asSubString != null && asString.IndexOf(asSubString, System.StringComparison.Ordinal) >= 0;

        /// <summary>cString::Sub: from start, count characters (all to the end if count is negative or too long).</summary>
        protected string StringSub(string asString, int alStart, int alCount)
        {
            if (string.IsNullOrEmpty(asString) || alStart >= asString.Length) return "";
            if (alStart < 0) alStart = 0;
            if (alCount < 0 || alStart + alCount > asString.Length) return asString.Substring(alStart);
            return asString.Substring(alStart, alCount);
        }

        // ------------------------------------------------------------------ timers

        protected void AddTimer(string asName, float afTime, string asFunction) => Map.AddTimer(asName, afTime, asFunction);

        protected void RemoveTimer(string asName) => Map.RemoveTimer(asName);

        protected float GetTimerTimeLeft(string asName) => Map.GetTimerTimeLeft(asName);

        // ------------------------------------------------------------------ local variables (text, like HPL2)

        protected void SetLocalVarInt(string asName, int alVal) => SetLocalVarString(asName, HplMath.ToString(alVal));

        protected void SetLocalVarFloat(string asName, float afVal) => SetLocalVarString(asName, HplMath.ToString(afVal));

        protected void SetLocalVarString(string asName, string asVal) => Map.SetVar(asName, asVal ?? "");

        protected void AddLocalVarInt(string asName, int alVal) =>
            Map.SetVar(asName, HplMath.ToString(HplMath.Atoi(Map.GetVar(asName)) + alVal));

        protected void AddLocalVarFloat(string asName, float afVal) =>
            Map.SetVar(asName, HplMath.ToString(HplMath.Atof(Map.GetVar(asName)) + afVal));

        protected void AddLocalVarString(string asName, string asVal) => Map.SetVar(asName, Map.GetVar(asName) + asVal);

        protected int GetLocalVarInt(string asName)
        {
            string s = Map.GetVar(asName);
            return s == "" ? 0 : HplMath.Atoi(s);
        }

        protected float GetLocalVarFloat(string asName)
        {
            string s = Map.GetVar(asName);
            return s == "" ? 0f : HplMath.Atof(s);
        }

        protected string GetLocalVarString(string asName) => Map.GetVar(asName);

        // ------------------------------------------------------------------ global variables

        protected void SetGlobalVarInt(string asName, int alVal) => SetGlobalVarString(asName, HplMath.ToString(alVal));

        protected void SetGlobalVarFloat(string asName, float afVal) => SetGlobalVarString(asName, HplMath.ToString(afVal));

        protected void SetGlobalVarString(string asName, string asVal) => HplGame.SetGlobalVar(asName, asVal ?? "");

        protected void AddGlobalVarInt(string asName, int alVal) =>
            HplGame.SetGlobalVar(asName, HplMath.ToString(HplMath.Atoi(HplGame.GetGlobalVar(asName)) + alVal));

        protected void AddGlobalVarFloat(string asName, float afVal) =>
            HplGame.SetGlobalVar(asName, HplMath.ToString(HplMath.Atof(HplGame.GetGlobalVar(asName)) + afVal));

        protected void AddGlobalVarString(string asName, string asVal) => HplGame.SetGlobalVar(asName, HplGame.GetGlobalVar(asName) + asVal);

        protected int GetGlobalVarInt(string asName)
        {
            string s = HplGame.GetGlobalVar(asName);
            return s == "" ? 0 : HplMath.Atoi(s);
        }

        protected float GetGlobalVarFloat(string asName)
        {
            string s = HplGame.GetGlobalVar(asName);
            return s == "" ? 0f : HplMath.Atof(s);
        }

        protected string GetGlobalVarString(string asName) => HplGame.GetGlobalVar(asName);

        // ------------------------------------------------------------------ game flow

        protected void StartCredits(string asMusic, bool abLoopMusic, string asTextCat, string asTextEntry, int alEndNum) =>
            NotPorted(nameof(StartCredits), "credits (step 6, menus)");

        protected void AddKeyPart(int alKeyPart) => NotPorted(nameof(AddKeyPart), "credits (step 6, menus)");

        protected void StartDemoEnd() { }

        protected void AutoSave() => NotPorted(nameof(AutoSave), "saving (step 6)");

        /// <summary>cLuxMap::SetCheckPoint + the death hint (cLuxPlayerDeath::SetHint).</summary>
        protected void CheckPoint(string asName, string asStartPos, string asCallback, string asDeathHintCat, string asDeathHintEntry)
        {
            Map.SetCheckPoint(asName, asStartPos, asCallback);
            SetDeathHint(asDeathHintCat, asDeathHintEntry);
        }

        protected void ChangeMap(string asMapName, string asStartPos, string asStartSound, string asEndSound) =>
            Map.ChangeMap(asMapName, asStartPos, asStartSound, asEndSound);

        protected void ClearSavedMaps() => HplSavedMaps.Reset();

        protected void CreateDataCache() { }

        protected void DestroyDataCache() { }

        protected void SetMapDisplayNameEntry(string asNameEntry) => Map.DisplayNameEntry = asNameEntry ?? "";

        protected void SetSkyBoxActive(bool abActive) => NotPorted(nameof(SetSkyBoxActive), "sky boxes");

        protected void SetSkyBoxTexture(string asTexture) => NotPorted(nameof(SetSkyBoxTexture), "sky boxes");

        protected void SetSkyBoxColor(float afR, float afG, float afB, float afA) => NotPorted(nameof(SetSkyBoxColor), "sky boxes");

        protected void UnlockAchievement(string asName) => Debug.Log($"[Script] Achievement '{asName}'");

        protected void SetFogActive(bool abActive) => Map.SetFog(active: abActive);

        protected void SetFogColor(float afR, float afG, float afB, float afA) => Map.SetFog(color: new Color(afR, afG, afB, afA));

        /// <summary>Start, falloff and culling are kept; the HDRP fog is matched at the fog end.</summary>
        protected void SetFogProperties(float afStart, float afEnd, float afFalloffExp, bool abCulling) =>
            Map.SetFog(start: afStart, end: afEnd, falloffExp: afFalloffExp);

        protected void SetupLoadScreen(string asTextCat, string asTextEntry, int alRandomNum, string asImageFile) =>
            Ui.HplLoadScreen.SetupLoadText(asTextCat, asTextEntry, alRandomNum, asImageFile);

        // ------------------------------------------------------------------ math and conversions

        protected float MathSin(float afX) => Mathf.Sin(afX);
        protected float MathCos(float afX) => Mathf.Cos(afX);
        protected float MathTan(float afX) => Mathf.Tan(afX);
        protected float MathAsin(float afX) => Mathf.Asin(afX);
        protected float MathAcos(float afX) => Mathf.Acos(afX);
        protected float MathAtan(float afX) => Mathf.Atan(afX);
        protected float MathAtan2(float afX, float afY) => Mathf.Atan2(afX, afY);
        protected float MathSqrt(float afX) => Mathf.Sqrt(afX);
        protected float MathPow(float afBase, float afExp) => Mathf.Pow(afBase, afExp);
        protected float MathMin(float afA, float afB) => Mathf.Min(afA, afB);
        protected float MathMax(float afA, float afB) => Mathf.Max(afA, afB);
        protected float MathClamp(float afX, float afMin, float afMax) => Mathf.Clamp(afX, afMin, afMax);
        protected float MathAbs(float afX) => Mathf.Abs(afX);
        protected int StringToInt(string asString) => HplMath.Atoi(asString);
        protected float StringToFloat(string asString) => HplMath.Atof(asString);
        protected bool StringToBool(string asString) => string.Equals(asString, "true", System.StringComparison.OrdinalIgnoreCase);
    }
}
