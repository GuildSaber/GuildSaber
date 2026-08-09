import Image from "@/components/Image"
import { Popover, PopoverTrigger } from "@/components/ui/popover"
import { Separator } from "@/components/ui/separator"
import { useSession } from "@/features/auth/hooks/useSession"
import MenuGuildsSelector from "@/features/guilds/components/GuildSelector/MenuGuildsSelector"
import { useGuildsStore } from "@/features/guilds/stores/guildsStore"
import { getCdnUrl } from "@/utils/url"
import { ChevronDown } from "lucide-react"
import { type RefObject, useRef } from "react"
import { useNavigate } from "react-router"
import { useResizeObserver } from "usehooks-ts"

// Size-10 slot + gap-2 between slots, in px
const SLOT_SIZE = 40
const SLOT_GAP = 8

const GuildsSelector = () => {
  const { data: session } = useSession()
  const router = useNavigate()
  const { selectedGuild, setSelectedGuild } = useGuildsStore()

  const fillerRef = useRef<HTMLDivElement>(null)
  const { width } = useResizeObserver({ ref: fillerRef as RefObject<HTMLDivElement>, box: "content-box" })

  const maxSlots = width === undefined ? 2 : Math.max(0, Math.floor((width + SLOT_GAP) / (SLOT_SIZE + SLOT_GAP)))
  const displayGuilds = session?.members.slice(0, maxSlots)
  const emptySlots = Math.max(0, maxSlots - (displayGuilds?.length ?? 0))

  const handleSelectGuild = (guildId: number) => () => {
    setSelectedGuild(guildId)
    router(`/guilds/${guildId}`)
  }

  if (!session) {
    return null
  }

  return (
    <div className="bg-background dark:bg-input/30 flex h-12 w-full items-center rounded-lg border shadow-xs">
      {session.members.length > 2 && selectedGuild && (
        <div className="flex h-full shrink-0 items-center gap-2 px-1 py-1">
          <img className="size-9 rounded-lg" src={getCdnUrl(`guilds/${selectedGuild}/logo.jpg`)} />
          <Separator orientation="vertical" className="bg-input" />
        </div>
      )}
      <div ref={fillerRef} className="flex h-full min-w-0 flex-1 items-center gap-2 overflow-hidden px-1 py-1">
        {Array.from({ length: emptySlots }, (_, i) => (
          <div key={i} className="border-input bg-background/40 size-10 shrink-0 rounded-lg border" />
        ))}
        {displayGuilds?.map((guild) => (
          <Image
            key={guild.guildId}
            onClick={handleSelectGuild(guild.guildId)}
            className="size-10 shrink-0 cursor-pointer rounded-lg"
            src={getCdnUrl(`guilds/${guild.guildId}/logo.jpg`)}
          />
        ))}
      </div>
      <Popover>
        <PopoverTrigger className="group shrink-0" asChild>
          <button className="my-1 mr-1 ml-auto flex size-10 items-center justify-center">
            <ChevronDown className="text-muted-foreground size-6 transition group-data-[state=open]:rotate-180" />
          </button>
        </PopoverTrigger>
        <MenuGuildsSelector />
      </Popover>
    </div>
  )
}

export default GuildsSelector
