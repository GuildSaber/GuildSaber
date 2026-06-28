const DATE_UNITS = [
  { limit: 60, divisor: 1, short: "s", long: "second" },
  { limit: 3600, divisor: 60, short: "m", long: "minute" },
  { limit: 86400, divisor: 3600, short: "h", long: "hour" },
  { limit: 2592000, divisor: 86400, short: "d", long: "day" },
  { limit: 31104000, divisor: 2592000, short: "mo", long: "month" },
  { limit: Infinity, divisor: 31104000, short: "y", long: "year" },
] as const

export const formatDate = (dateStr: string, mode: "short" | "long" = "short") => {
  const diff = (Date.now() - new Date(dateStr).getTime()) / 1000

  if (diff < 60) {
    return "just now"
  }

  const unit = DATE_UNITS.find((u) => diff < u.limit) ?? DATE_UNITS[DATE_UNITS.length - 1]
  const value = Math.floor(diff / unit.divisor)

  if (mode === "short") {
    return `${value}${unit.short}`
  }

  return `${value} ${unit.long}${value > 1 ? "s" : ""} ago`
}

export const formatTime = (seconds: number) => {
  const hours = Math.floor(seconds / 3600)
  const mins = Math.floor((seconds % 3600) / 60)
  const secs = seconds % 60

  if (hours > 0) {
    return `${hours}:${mins.toString().padStart(2, "0")}:${secs.toString().padStart(2, "0")}`
  }

  return `${mins}:${secs.toString().padStart(2, "0")}`
}
