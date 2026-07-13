import { cn } from "@/lib/utils"
import React from "react"

interface BadgeStatProps {
  icon?: React.ReactNode
  label: string
  className?: string
}

export const BadgeStat = ({ icon, label, className }: BadgeStatProps) => (
  <div
    className={cn(
      "border-input flex items-center gap-1 rounded border px-1.5 py-0.5 text-sm md:px-2 md:py-1",
      className,
    )}
  >
    {icon}
    <span>{label}</span>
  </div>
)
