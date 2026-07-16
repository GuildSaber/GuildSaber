export { formatDate } from "@/utils/time"

export const formatScore = (score: number | string) => Number(score).toLocaleString()

export const formatPoints = (points: number | string) => parseFloat(String(points)).toFixed(2)
