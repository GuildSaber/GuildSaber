import type { ScoreId } from "@/client"
import { getScoreStatisticsOptions } from "@/client/@tanstack/react-query.gen"
import { useQuery } from "@tanstack/react-query"

interface Params {
  scoreId: ScoreId
  enabled: boolean
}

export const useMapScoreStats = ({ scoreId, enabled }: Params) =>
  useQuery({
    ...getScoreStatisticsOptions({ path: { scoreId } }),
    enabled,
  })
