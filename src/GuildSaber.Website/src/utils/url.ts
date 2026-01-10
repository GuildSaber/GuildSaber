export const getApiUrl = (): string => {
  const { protocol, hostname } = window.location

  // Local
  if (hostname === "localhost") {
    return `${protocol}//localhost:5042`
  }

  // Developpement
  if (hostname.startsWith("dev.")) {
    const domain = hostname.substring(4)

    return `${protocol}//api-dev.${domain}`
  }

  // Production
  return `${protocol}//api.${hostname}`
}
