import { client } from "@/client/client.gen"
import { getApiUrl } from "@/utils/url"

client.setConfig({
  baseUrl: getApiUrl(),
  credentials: "include",
})

client.interceptors.request.use((request) => {
  if (!["GET", "HEAD", "OPTIONS"].includes(request.method)) {
    request.headers.set("X-GuildSaber-Request", "1")
  }

  return request
})
