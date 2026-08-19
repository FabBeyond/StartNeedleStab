using BepInEx;
using HarmonyLib;
using HutongGames.PlayMaker;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StartNeedleStab
{
    [BepInAutoPlugin(id: "io.github.fabbeyond.startneedlestab")]
    public partial class StartNeedleStabPlugin : BaseUnityPlugin
    {
        public static StartNeedleStabPlugin instance;
        public bool kill = false;
        public void Awake()
        {
            if (Random.Range(0, 2) == 0) kill = true;
            
            instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Harmony harmony = new Harmony("io.github.fabbeyond.startneedlestab");
            harmony.PatchAll();
        }
        public void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!kill) return;
            if (scene.name != "Tut_01") return;

            GameObject needle = GameObject.Find("Tutorial Intro Cutscene");
            if (needle == null) return;

            foreach (Transform child in needle.transform)
            {
                if (child.name == "Needle" || child.name == "Crumbly Dust" || child.name == "Thunk Effect")
                {
                    child.localPosition = new Vector3(child.localPosition.x + 0.9f, child.localPosition.y, child.localPosition.z);
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayMakerFSM), "OnEnable")]
    public static class IntroFSMPatch
    {
        static void Postfix(PlayMakerFSM __instance)
        {
            if (!StartNeedleStabPlugin.instance.kill) return;

            if (__instance.FsmName == "Control" && __instance.gameObject.name == "Tutorial Intro Cutscene")
            {
                FsmState state = __instance.Fsm.GetState("Needle Land");
                if (state == null) return;

                InsertAction(state, new KillHornet(), 0);
            }
            if (__instance.FsmName == "Hero Death Anim")
            {
                FsmState state = __instance.Fsm.GetState("Ended");
                if (state == null) return;

                InsertAction(state, new StopGame(), 0);
            }
        }

        static void InsertAction(FsmState state, FsmStateAction action, int index)
        {
            var actions = state.Actions.ToList();
            actions.Insert(index, action);
            state.Actions = actions.ToArray();
            action.Init(state);
        }
    }

    public class KillHornet : FsmStateAction
    {
        public override void OnEnter()
        {
            HeroController.instance.TakeDamage(null, GlobalEnums.CollisionSide.other, 99, GlobalEnums.HazardType.NON_HAZARD);
            Finish();
        }
    }
    public class StopGame : FsmStateAction
    {
        public override void OnEnter()
        {
            Application.Quit();
        }
    }
}
