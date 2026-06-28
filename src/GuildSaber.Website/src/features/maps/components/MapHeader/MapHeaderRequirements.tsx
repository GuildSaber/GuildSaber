import type { RankedMap, RankedMapRequirements } from "@/client"

interface Props {
  map: RankedMap
}

const RequirementBadge = ({ label, value }: { label: string; value: string }) => (
  <div className="border-input flex items-center overflow-hidden rounded border text-xs sm:text-sm">
    <span className="border-input bg-input/30 border-r px-2 py-1 font-medium sm:px-3 sm:py-1.5">{label}</span>
    <span className="px-2 py-1 sm:px-3 sm:py-1.5">{value}</span>
  </div>
)

const isDefined = <T,>(v: T | null | undefined): v is T => v !== null && v !== undefined

const formatModifiers = (modifiers: string) =>
  modifiers
    .split(",")
    .map((m) => m.trim())
    .join(" | ")

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
      show: req.mandatoryModifiers,
      label: "Mandatory",
      value: formatModifiers(req.mandatoryModifiers ?? ""),
    },
    {
      show: req.prohibitedModifiers,
      label: "Not Allowed",
      value: formatModifiers(req.prohibitedModifiers ?? ""),
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
        {badges.map(({ label, value }) => (
          <RequirementBadge key={label} label={label} value={value} />
        ))}
      </div>
    </div>
  )
}
