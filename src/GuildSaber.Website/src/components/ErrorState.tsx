import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"
import { Home, RefreshCw, ServerCrash, type LucideIcon } from "lucide-react"
import { useNavigate } from "react-router"

const variants = {
  error: { container: "bg-destructive/10", icon: "text-destructive" },
  warning: { container: "bg-amber-500/10", icon: "text-amber-500" },
  info: { container: "bg-primary/10", icon: "text-primary" },
  muted: { container: "bg-muted", icon: "text-muted-foreground" },
} as const

interface Action {
  label: string
  icon?: LucideIcon
  onClick: () => void
  variant?: "default" | "outline" | "destructive" | "secondary" | "ghost" | "link"
}

interface Props {
  icon?: LucideIcon
  variant?: keyof typeof variants
  title?: string
  description?: string
  actions?: Action[]
  showDefaultActions?: boolean
  className?: string
}

const ErrorState = ({
  icon: Icon = ServerCrash,
  variant = "error",
  title = "Oops! Something went wrong",
  description = "We encountered an unexpected error. Please try again or return to the home page.",
  actions,
  showDefaultActions = true,
  className,
}: Props) => {
  const navigate = useNavigate()
  const styles = variants[variant]

  const defaultActions: Action[] = [
    {
      label: "Try again",
      icon: RefreshCw,
      onClick: () => window.location.reload(),
      variant: "outline",
    },
    {
      label: "Go home",
      icon: Home,
      onClick: () => void navigate("/"),
      variant: "default",
    },
  ]

  const displayActions = actions ?? (showDefaultActions ? defaultActions : [])

  return (
    <div className={cn("flex flex-1 flex-col items-center justify-center gap-6 p-8", className)}>
      <div className={cn("flex size-24 items-center justify-center rounded-full", styles.container)}>
        <Icon className={cn("size-12", styles.icon)} />
      </div>

      <div className="text-center">
        <h1 className="text-foreground mb-2 text-2xl font-bold">{title}</h1>
        <p className="text-muted-foreground max-w-sm">{description}</p>
      </div>

      {displayActions.length > 0 && (
        <div className="flex gap-3">
          {displayActions.map((action) => (
            <Button key={action.label} variant={action.variant ?? "default"} onClick={action.onClick}>
              {action.icon && <action.icon className="size-4" />}
              {action.label}
            </Button>
          ))}
        </div>
      )}
    </div>
  )
}

export default ErrorState
