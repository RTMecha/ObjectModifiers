using EventKeyframe = DataManager.GameData.EventKeyframe;
using BeatmapObject = DataManager.GameData.BeatmapObject;

namespace ObjectModifiers.Functions
{
	public static class Triggers
	{
		public static float EventValuesZ1(EventKeyframe _posEvent)
		{
			BeatmapObject bo = null;
			if (DataManager.inst.gameData.beatmapObjects.Find((BeatmapObject x) => x.events[0].Contains(_posEvent)) != null)
			{
				bo = DataManager.inst.gameData.beatmapObjects.Find((BeatmapObject x) => x.events[0].Contains(_posEvent));
			}
			float z = 0.0005f * bo.Depth;
			if (_posEvent.eventValues.Length > 2 && bo != null)
			{
				float calc = _posEvent.eventValues[2] / 10f;
				z = z + calc;
			}
			return z;
		}

		public static float EventValuesZ2(EventKeyframe _posEvent)
		{
			BeatmapObject bo = null;
			if (DataManager.inst.gameData.beatmapObjects.Find((BeatmapObject x) => x.events[0].Contains(_posEvent)) != null)
			{
				bo = DataManager.inst.gameData.beatmapObjects.Find((BeatmapObject x) => x.events[0].Contains(_posEvent));
			}
			float z = 0.1f * bo.Depth;
			if (_posEvent.eventValues.Length > 2 && bo != null)
			{
				float calc = _posEvent.eventValues[2] / 10f;
				z = z + calc;
			}
			return z;
		}

		public static float DummyNumber(EventKeyframe _posEvent)
		{
			float z = 0.0005f;
			return z;
		}
	}
}
