export const generateAuthUrl = (provider: "beatleader" | "discord", method: "login" | "link") =>
  `${import.meta.env.VITE_API_URL}/auth/${method}/${provider}?returnUrl=${import.meta.env.VITE_WEBSITE_URL}/auth`
