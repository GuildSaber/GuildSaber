import { keepPreviousData, QueryClient } from "@tanstack/react-query"

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 5,
      staleTime: 60_000,
      placeholderData: keepPreviousData,
    },
  },
})

export default queryClient
