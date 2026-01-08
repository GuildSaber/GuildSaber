import { getPlayerAtMeOptions } from "@/client/@tanstack/react-query.gen"
import { useQuery } from "@tanstack/react-query"

export const useSession = () =>
  useQuery({
    ...getPlayerAtMeOptions(),
    staleTime: 60 * 1_000,
    retry: 0,
  })
