import Discord from "@/components/icons/Discord"
import { Button } from "@/components/ui/button"
import { useSession } from "@/features/auth/hooks/useSession"
import { generateAuthUrl } from "@/features/auth/utils"
import { cn } from "@/lib/utils"
import { Check, CircleCheck, Link as LinkIcon } from "lucide-react"
import { Link } from "react-router"
import JoinCSGuild from "./JoinCSGuild"
import DiscordLinkUser from "./providers/DiscordLinkProvider"

interface StepIndicatorProps {
  step: number
  done: boolean
  active: boolean
}

const StepIndicator = ({ step, done, active }: StepIndicatorProps) => (
  <div
    className={cn(
      "flex h-6 w-6 shrink-0 items-center justify-center rounded-full border-2 text-xs font-semibold transition-colors",
      done && "border-green-500 bg-green-500 text-white",
      active && !done && "border-primary bg-primary",
      !done && !active && "border-muted bg-muted text-muted-foreground",
    )}
  >
    {done ? <Check className="size-3" /> : step}
  </div>
)

interface StepContentProps {
  isMember: boolean
  hasDiscord: boolean
  discordId: string
}

const StepContent = ({ isMember, hasDiscord, discordId }: StepContentProps) => {
  const handleRelink = () => {
    window.location.href = generateAuthUrl("discord", "link")
  }

  if (!isMember) {
    return <JoinCSGuild />
  }

  if (!hasDiscord) {
    return <DiscordLinkUser />
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center gap-3">
        <img className="size-20 rounded-lg" src="https://cdn-dev.guildsaber.com/guilds/1/logo.jpg" />
        <div>
          <p className="text-2xl font-semibold">Challenge Saber</p>
          <p className="flex items-center text-green-500">
            <CircleCheck className="mr-1 inline h-5 w-5" />
            Joined
          </p>
          <Link className="text-primary mb-2 underline" to="/guilds/1">
            View guild page
          </Link>
        </div>
      </div>
      <div className="flex items-center gap-3">
        <div className="flex size-20 shrink-0 items-center justify-center rounded-lg bg-[#5865F2]">
          <Discord className="h-12 w-12 fill-white" />
        </div>
        <div>
          <p className="text-gray-200">
            Discord ID: <span className="rounded bg-gray-800 px-1 font-mono text-white">{discordId}</span>
          </p>
          <p className="flex items-center text-green-500">
            <CircleCheck className="mr-1 inline h-5 w-5" />
            Linked
          </p>
          <Button className="mt-1" size="sm" variant="outline" onClick={handleRelink}>
            <LinkIcon className="inline h-4 w-4" />
            Relink
          </Button>
        </div>
      </div>
    </div>
  )
}

const AuthSetupStepper = () => {
  const { data: session } = useSession()

  if (!session) {
    return null
  }

  const isMember = session.members.some((m) => m.guildId === 1)
  const discordId = session.player.playerLinkedAccounts.discordId ?? ""
  const hasDiscord = Boolean(discordId)
  let progress = 0

  if (isMember) {
    progress = hasDiscord ? 100 : 50
  }

  return (
    <div className="flex flex-col gap-6">
      <StepContent isMember={isMember} hasDiscord={hasDiscord} discordId={discordId} />

      {!(isMember && hasDiscord) && (
        <div className="relative mx-auto flex w-full max-w-50 items-start justify-between">
          <div className="absolute inset-x-3 top-3 -translate-y-px">
            <div className="bg-border h-0.5 overflow-hidden rounded-full">
              <div className="h-full bg-green-500 transition-all duration-500" style={{ width: `${progress}%` }} />
            </div>
          </div>

          <div className="relative flex flex-col items-center gap-1">
            <StepIndicator step={1} done={isMember} active={!isMember} />
            <span className={cn("text-xs", isMember ? "text-green-500" : "text-muted-foreground")}>Guild</span>
          </div>
          <div className="relative flex flex-col items-center gap-1">
            <StepIndicator step={2} done={isMember && hasDiscord} active={isMember && !hasDiscord} />
            <span className={cn("text-xs", isMember && hasDiscord ? "text-green-500" : "text-muted-foreground")}>
              Discord
            </span>
          </div>
        </div>
      )}
    </div>
  )
}

export default AuthSetupStepper
