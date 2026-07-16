import { cn } from "@/lib/utils"
import React from "react"

interface BadgeProps {
  className?: string
  style?: React.CSSProperties
  children: React.ReactNode
}

export const Badge = React.forwardRef<HTMLDivElement, BadgeProps>(({ children, className, style }, ref) => (
  <div
    ref={ref}
    style={style}
    className={cn(
      "border-input flex items-center gap-1 rounded border px-1.5 py-0.5 text-sm md:px-2 md:py-1",
      className,
    )}
  >
    {children}
  </div>
))

Badge.displayName = "Badge"
