using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using NotOutYet.Configuration;
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using IPALogger = IPA.Logging.Logger;

namespace NotOutYet
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }
        internal static Harmony HarmonyInstance { get; private set; }
        public readonly string HarmonyID = "Saeraphinx.NotOutYet";
        internal static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        [Init]
        public Plugin(IPALogger logger)
        {
            Instance = this;
            Plugin.Log = logger;
            Plugin.Log?.Debug("Logger initialized.");
        }

        [Init]
        public void InitWithConfig(Config conf)
        {
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
        }


        #region Disableable
        [OnEnable]
        public async void OnEnable()
        {
            HarmonyInstance = new Harmony(HarmonyID);
            ApplyHarmonyPatches();
            if (PluginConfig.Instance.Regex == "DefaultUsername")
            {
                try
                {
                    var user = await BS_Utils.Gameplay.GetUserInfo.GetUserAsync();
                    PluginConfig.Instance.Regex = user.userName;
                } catch (Exception ex)
                {
                    Plugin.Log?.Warn("Error fetching user info: " + ex.Message);
                    Plugin.Log?.Debug(ex);
                }
            }
        }
        [OnDisable]
        public void OnDisable()
        {
            RemoveHarmonyPatches();
        }
        #endregion

        #region Harmony
        internal static void ApplyHarmonyPatches()
        {
            try
            {
                Plugin.Log?.Debug("Applying Harmony patches.");
                HarmonyInstance.PatchAll(Assembly);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error("Error applying Harmony patches: " + ex.Message);
                Plugin.Log?.Debug(ex);
            }
        }
        internal static void RemoveHarmonyPatches()
        {
            try
            {
                // Removes all patches with this HarmonyId
                HarmonyInstance.UnpatchSelf();
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error("Error removing Harmony patches: " + ex.Message);
                Plugin.Log?.Debug(ex);
            }
        }
        #endregion
    }
}

namespace NotOutYet.HarmonyPatches
{
    [HarmonyPatch(typeof(TMP_Text))]
    [HarmonyPatch("text", MethodType.Setter)]
    class MeshTextProReplaceSetText
    {
        static readonly Regex regex = new Regex(PluginConfig.Instance.Regex, RegexOptions.IgnoreCase);

        [HarmonyPrefix]
        static bool Prefix(ref string value)
        {
            try
            {
                if (value == null)
                {
                    return true;
                }
                value = regex.Replace(value, PluginConfig.Instance.ReplacementText);
                return true;
            } catch (Exception ex)
            {
                Plugin.Log?.Error("Error replacing text: " + ex.Message);
                Plugin.Log?.Debug(ex);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(TextMeshProUGUI))]
    [HarmonyPatch("Awake")]
    class MeshTextProReplaceAwake
    {
        static readonly Regex regex = new Regex(PluginConfig.Instance.Regex, RegexOptions.IgnoreCase);

        [HarmonyPostfix]
        static void Postfix(ref TextMeshProUGUI __instance)
        {
            try
            {
                if (__instance.text == null)
                {
                    return;
                }
                __instance.text = regex.Replace(__instance.text, PluginConfig.Instance.ReplacementText);
            } catch (Exception ex)
            {
                Plugin.Log?.Error("Error replacing text: " + ex.Message);
                Plugin.Log?.Debug(ex);
            }
        }
    }
}
