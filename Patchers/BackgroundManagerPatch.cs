using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using UnityEngine;

namespace ObjectModifiers.Patchers
{
	[HarmonyPatch(typeof(BackgroundManager))]
    public class BackgroundManagerPatch : MonoBehaviour
    {
		public static IEnumerator LoadBackground(BackgroundManager __instance)
		{
			float delay = 0f;
			while (GameManager.inst.gameState != GameManager.State.Playing)
			{
				yield return new WaitForSeconds(delay);
				delay += 0.0001f;
			}

			audio = AudioManager.inst.CurrentAudioSource;

			__instance.samples = new float[256];

			var clip = audio.clip;
			if (clip != null)
			{
				clip.GetData(__instance.samples, 0);
			}

			Debug.Log($"{ObjectModifiersPlugin.className}Begin loading BG Objects: {DataManager.inst.gameData.backgroundObjects}");
			foreach (var background in DataManager.inst.gameData.backgroundObjects)
			{
				var bg = new BG(new BG.Reactive(new int[2], new float[2]), background);
				Debug.Log($"{ObjectModifiersPlugin.className}Loading new BG: {background} {bg}");
				ObjectModifiersPlugin.backgrounds.Add(bg);
			}

			foreach (var background in DataManager.inst.gameData.backgroundObjects)
			{
				__instance.CreateBackgroundObject(background);
			}
			yield break;
		}


		[HarmonyPatch("CreateBackgroundObject")]
		[HarmonyPrefix]
		static bool CreateBackgroundObject(BackgroundManager __instance, ref GameObject __result, DataManager.GameData.BackgroundObject __0)
		{
			GameObject gameObject = Instantiate(__instance.backgroundPrefab, new Vector3(__0.pos.x, __0.pos.y, (float)(32 + __0.layer * 10)), Quaternion.identity);
			gameObject.name = __0.name;
			gameObject.isStatic = true;
			gameObject.transform.SetParent(__instance.backgroundParent);
			gameObject.transform.localScale = new Vector3(__0.scale.x, __0.scale.y, 10f);
			gameObject.layer = 9;
			gameObject.GetComponent<SelectBackgroundInEditor>().obj = __instance.backgroundObjects.Count;
			__instance.backgroundObjects.Add(gameObject);

			if (__0.drawFade)
			{
				int num = 9;
				for (int i = 1; i < num - __0.layer; i++)
				{
					GameObject gameObject2 = Instantiate(__instance.backgroundFadePrefab, Vector3.zero, Quaternion.identity);
					gameObject2.isStatic = true;
					gameObject2.name = string.Concat(new object[]
					{
						__0.name,
						" Fade [",
						i,
						"]"
					});
					gameObject2.transform.SetParent(gameObject.transform);
					gameObject2.transform.localPosition = new Vector3(0f, 0f, (float)i);
					gameObject2.transform.localScale = new Vector3(1f, 1f, 1f);
					gameObject2.layer = 9;
				}
			}
			gameObject.transform.Rotate(new Vector3(0f, 0f, __0.rot));

			__result = gameObject;

			return false;
		}

		[HarmonyPatch("Update")]
		[HarmonyPrefix]
		static bool UpdatePatch(BackgroundManager __instance)
        {
			if (audio != null)
            {
				UpdateBackgroundObjects(__instance);
            }
			return false;
        }

		[HarmonyPatch("UpdateBackgroundObjects")]
		[HarmonyPrefix]
		static bool UpdateBackgroundObjects(BackgroundManager __instance)
		{
			if (GameManager.inst.gameState == GameManager.State.Playing)
			{
				AudioManager.inst.CurrentAudioSource.GetSpectrumData(__instance.samples, 0, 0);
				__instance.sampleLow = __instance.samples.Skip(0).Take(56).Average((float a) => a) * 1000f;
				__instance.sampleMid = __instance.samples.Skip(56).Take(100).Average((float a) => a) * 3000f;
				__instance.sampleHigh = __instance.samples.Skip(156).Take(100).Average((float a) => a) * 6000f;
				int num = 0;

				foreach (var bg in ObjectModifiersPlugin.backgrounds)
                {
					var backgroundObject = bg.bgObject;

					if (backgroundObject.reactive)
					{
						switch (backgroundObject.reactiveType)
						{
							case DataManager.GameData.BackgroundObject.ReactiveType.LOW:
								backgroundObject.reactiveSize = new Vector2(__instance.sampleLow, __instance.sampleLow) * backgroundObject.reactiveScale;
								break;
							case DataManager.GameData.BackgroundObject.ReactiveType.MID:
								backgroundObject.reactiveSize = new Vector2(__instance.sampleMid, __instance.sampleMid) * backgroundObject.reactiveScale;
								break;
							case DataManager.GameData.BackgroundObject.ReactiveType.HIGH:
								backgroundObject.reactiveSize = new Vector2(__instance.sampleHigh, __instance.sampleHigh) * backgroundObject.reactiveScale;
								break;
						}
						if (__instance.backgroundObjects.Count > num)
						{
							__instance.backgroundObjects[num].transform.localScale = new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, 10f) + new Vector3(backgroundObject.reactiveSize.x, backgroundObject.reactiveSize.y);
						}
					}
					else
					{
						float x = __instance.samples[bg.reactive.channels[0]];
						float y = __instance.samples[bg.reactive.channels[1]];
						backgroundObject.reactiveSize = new Vector2(x * bg.reactive.values[0], y * bg.reactive.values[1]);
						if (__instance.backgroundObjects.Count > num)
						{
							__instance.backgroundObjects[num].transform.localScale = new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, 10f) + new Vector3(backgroundObject.reactiveSize.x, backgroundObject.reactiveSize.y);
						}
					}
					num++;
				}
			}
			return false;
		}

		[HarmonyPatch("LoadBackground")]
		[HarmonyPrefix]
		static bool LoadBackgroundPatch(BackgroundManager __instance)
        {
			ObjectModifiersPlugin.inst.StartCoroutine(LoadBackground(__instance));
			return false;
        }

		[HarmonyPatch("UpdateBackgrounds")]
		[HarmonyPrefix]
		static bool UpdateBackgrounds(BackgroundManager __instance)
		{
			foreach (GameObject gameObject in __instance.backgroundObjects)
			{
				Destroy(gameObject);
			}
			__instance.backgroundObjects.Clear();

			foreach (var background in DataManager.inst.gameData.backgroundObjects)
            {
				if (ObjectModifiersPlugin.backgrounds.Find((BG x) => x.bgObject == background) == null)
                {
					ObjectModifiersPlugin.backgrounds.Add(new BG(new BG.Reactive(new int[2], new float[2]), background));
				}
            }

			foreach (DataManager.GameData.BackgroundObject background in DataManager.inst.gameData.backgroundObjects)
			{
				__instance.CreateBackgroundObject(background);
			}
			return false;
		}

		public static AudioSource audio;
	}

	public class BG
    {
		public BG(Reactive _reactive, DataManager.GameData.BackgroundObject backgroundObject)
        {
			reactive = _reactive;
			bgObject = backgroundObject;
		}

		public Reactive reactive;
		public DataManager.GameData.BackgroundObject bgObject;

		public class Reactive
        {
			public Reactive(int[] _channels, float[] _values)
            {
				channels = _channels;
				values = _values;
            }

			public int[] channels;
			public float[] values;
        }
    }
}
