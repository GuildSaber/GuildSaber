import { client } from "@/client/client.gen"

client.setConfig({
  baseUrl: import.meta.env.VITE_API_URL,
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
