import { getApiUrl } from "@/utils/url"

export const generateAuthUrl = (provider: "beatleader" | "discord", method: "login" | "link") =>
  `${getApiUrl()}/auth/${method}/${provider}?returnUrl=${encodeURIComponent(`${window.location.origin}/auth`)}`
