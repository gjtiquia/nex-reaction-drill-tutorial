using Cysharp.Threading.Tasks;

#if GAME_HUB_ENABLED
using System;
using System.Collections.Generic;
using Nex.Platform;
#endif

namespace Nex.Essentials
{
    public static class ReactionDrillGameHubWrapper
    {
        public static UniTask<bool> SubmitScore(int score)
        {
#if GAME_HUB_ENABLED
            return GameHub.Instance.SubmitMetrics(new List<GameHub.MetricEntry>
            {
                new() { name = "reaction_drill_game_count", value = 1 },
                new() { name = "reaction_drill_score", value = score },
            });
#else
            return UniTask.FromResult(true);
#endif
        }

        public static async UniTask<int?> QueryHighestScore()
        {
#if GAME_HUB_ENABLED
            const string highestScoreAchievementId = "e484bf0b-62cf-4849-97ee-7c1cd71a155a";
            var achievements = await GameHub.Instance.QueryAchievements(new List<string> { highestScoreAchievementId });
            return achievements.TryGetValue(highestScoreAchievementId, out var achievementData)
                ? Convert.ToInt32(achievementData.progress)
                : null;

#else
            return null;
#endif
        }

        public static bool IsAvailable()
        {
#if GAME_HUB_ENABLED
            return GameHub.Instance.IsAvailable;
#else
            return false;
#endif
        }
    }
}
