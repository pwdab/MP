using System;

namespace MP.Gameplay.Stages
{
    /*
        스테이지 진행 상태 스냅샷
        Network, 저장, 디버그 UI 등 외부 시스템이 StageFlowController 상태를 전달할 때 사용
    */
    [Serializable]
    public readonly struct StageSnapshot
    {
        public StageSnapshot(StageState stageState, WaveState waveState, int waveIndex, bool hasCurrentWave, float stageElapsedTime, float waveElapsedTime, int gold, int experience)
        {
            StageState = stageState;
            WaveState = waveState;
            WaveIndex = waveIndex;
            HasCurrentWave = hasCurrentWave;
            StageElapsedTime = stageElapsedTime;
            WaveElapsedTime = waveElapsedTime;
            Gold = gold;
            Experience = experience;
        }

        public StageState StageState { get; }
        public WaveState WaveState { get; }
        public int WaveIndex { get; }
        public bool HasCurrentWave { get; }
        public float StageElapsedTime { get; }
        public float WaveElapsedTime { get; }
        public int Gold { get; }
        public int Experience { get; }

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (!Enum.IsDefined(typeof(StageState), StageState))
            {
                reason = $"StageSnapshot has invalid stage state '{StageState}'.";
                return false;
            }

            if (!Enum.IsDefined(typeof(WaveState), WaveState))
            {
                reason = $"StageSnapshot has invalid wave state '{WaveState}'.";
                return false;
            }

            if (float.IsNaN(StageElapsedTime) || float.IsInfinity(StageElapsedTime)
                || float.IsNaN(WaveElapsedTime) || float.IsInfinity(WaveElapsedTime))
            {
                reason = "StageSnapshot has invalid elapsed time.";
                return false;
            }

            if (Gold < 0 || Experience < 0)
            {
                reason = "StageSnapshot has invalid resource values.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
