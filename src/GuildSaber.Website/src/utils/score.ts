import type { RankedMap, RankedScore } from "@/client"

export const getAccuracy = (rankedMap: RankedMap, rankedScore: RankedScore) => {
  const version = rankedMap.versions.find((v) => v.difficulty.id === rankedScore.score.songDifficultyId)
  const maxScore = Number(version?.difficulty.stats.maxScore ?? 0)

  if (maxScore === 0) {
    return 0
  }

  return (Number(rankedScore.effectiveScore) / maxScore) * 100
}
