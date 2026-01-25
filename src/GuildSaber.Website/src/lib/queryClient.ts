import { keepPreviousData, QueryClient } from "@tanstack/react-query"

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 0,
      staleTime: 60_000,
      placeholderData: keepPreviousData,
    },
  },
})

export default queryClient
