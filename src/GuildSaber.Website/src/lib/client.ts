import { client } from "@/client/client.gen"
import { getApiUrl } from "@/utils/url"

client.setConfig({
  baseUrl: getApiUrl(),
  headers: {
    Authorization: `Bearer ${localStorage.getItem("token")}`,
  },
  hooks: {
    afterResponse: [
      (_request, _options, response) => {
        if (response.status === 401) {
          localStorage.removeItem("token")
        }
      },
    ],
  },
})
