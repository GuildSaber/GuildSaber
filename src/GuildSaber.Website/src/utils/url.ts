export const getApiUrl = (): string => {
  const currentUrl = window.location.origin
  const lowerUrl = currentUrl.toLowerCase()

  if (lowerUrl.startsWith("http://localhost")) {
    return "http://localhost:5042"
  }

  if (lowerUrl.startsWith("http://dev.")) {
    return `http://api-dev.${new URL(currentUrl).host.substring(4)}`
  }

  if (lowerUrl.startsWith("http://")) {
    return `http://api.${new URL(currentUrl).host}`
  }

  return currentUrl
}
