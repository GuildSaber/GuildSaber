import { client } from "@/client/client.gen"
import { getApiUrl } from "@/utils/url"

client.setConfig({
  baseUrl: getApiUrl(),
})

client.interceptors.request.use((config) => {
  const token = localStorage.getItem("token")

  if (token) {
    config.headers.set("Authorization", `Bearer ${token}`)
  }

  return config
})
