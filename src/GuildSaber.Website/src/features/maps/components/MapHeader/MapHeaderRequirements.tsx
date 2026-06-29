import type { RankedMap, RankedMapRequirements } from "@/client"
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip"
import { PROHIBITED_DEFAULT_KEYS } from "@/utils/constants"
import { Info } from "lucide-react"
import { useState } from "react"

const MODIFIER_NONE = "None"
const MODIFIER_PROHIBITED_DEFAULTS = "ProhibitedDefaults"

interface Props {
  map: RankedMap
}

const Separator = () => <span className="border-border h-3.5 self-center border-l" />

const RequirementValue = ({ value, tooltip }: { value: string | string[]; tooltip?: string }) => {
  const [open, setOpen] = useState(false)

  if (tooltip) {
    return (
      <Tooltip open={open} onOpenChange={setOpen}>
        <TooltipTrigger asChild>
          <span
            className="flex cursor-default items-center px-2 py-1 sm:px-3 sm:py-1.5"
            onTouchEnd={(e) => {
              e.preventDefault()
              setOpen((v) => !v)
            }}
          >
            {value}

            <Info className="ml-1.5 inline size-3.5" />
          </span>
        </TooltipTrigger>
        <TooltipContent>{tooltip}</TooltipContent>
      </Tooltip>
    )
  }

  if (Array.isArray(value)) {
    return (
      <div className="flex items-center px-2 py-1 sm:px-3 sm:py-1.5">
        {value.map((v, i) => (
          <span key={v} className="flex items-center">
            {i > 0 && <Separator />}
            <span className={i > 0 ? "pl-1" : undefined}>{v}</span>
          </span>
        ))}
      </div>
    )
  }

  return <span className="px-2 py-1 sm:px-3 sm:py-1.5">{value}</span>
}

const RequirementBadge = ({ label, value, tooltip }: { label: string; value: string | string[]; tooltip?: string }) => (
  <div className="border-input flex items-center overflow-hidden rounded border text-xs sm:text-sm">
    <span className="bg-input/30 px-2 py-1 font-medium sm:px-3 sm:py-1.5">{label}</span>
    <Separator />
    <RequirementValue value={value} tooltip={tooltip} />
  </div>
)

const isDefined = <T,>(v: T | null | undefined): v is T => v !== null && v !== undefined

const formatModifiers = (modifiers: string): string[] => modifiers.split(",").map((m) => m.trim())

const getBadges = (req: RankedMapRequirements) =>
  [
    { show: req.needFullCombo, label: "Full Combo", value: "Required" },
    { show: req.needConfirmation, label: "Confirmation", value: "Required" },
    { show: isDefined(req.minAccuracy), label: "Min Accuracy", value: `${req.minAccuracy}%` },
    {
      show: isDefined(req.maxPauseDurationSec),
      label: "Total Pause",
      value: `<${req.maxPauseDurationSec}s`,
    },
    {
      show: req.mandatoryModifiers && req.mandatoryModifiers !== MODIFIER_NONE,
      label: "Mandatory",
      value: formatModifiers(req.mandatoryModifiers ?? ""),
    },
    {
      show: req.prohibitedModifiers,
      label: "Not Allowed",
      value:
        req.prohibitedModifiers === MODIFIER_PROHIBITED_DEFAULTS
          ? "Prohibited Defaults"
          : formatModifiers(req.prohibitedModifiers ?? ""),
      tooltip:
        req.prohibitedModifiers === MODIFIER_PROHIBITED_DEFAULTS ? PROHIBITED_DEFAULT_KEYS.join(" · ") : undefined,
    },
  ].filter(({ show }) => show)

export const MapHeaderRequirements = ({ map }: Props) => {
  const badges = getBadges(map.requirements)

  if (badges.length === 0) {
    return null
  }

  return (
    <div className="flex flex-col gap-3">
      <hr className="border-border" />
      <p className="text-muted-foreground text-xs font-semibold tracking-wider uppercase">Requirements</p>
      <div className="flex flex-wrap gap-2">
        {badges.map(({ label, value, tooltip }) => (
          <RequirementBadge key={label} label={label} value={value} tooltip={tooltip} />
        ))}
      </div>
    </div>
  )
}
