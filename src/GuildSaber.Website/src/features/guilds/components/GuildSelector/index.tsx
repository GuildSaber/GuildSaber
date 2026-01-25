import { Popover, PopoverTrigger } from "@/components/ui/popover"
import { Separator } from "@/components/ui/separator"
import { useSession } from "@/features/auth/hooks/useSession"
import MenuGuildsSelector from "@/features/guilds/components/GuildSelector/MenuGuildsSelector"
import { useGuildsStore } from "@/features/guilds/stores/guildsStore"
import { getCdnUrl } from "@/utils/url"
import { ChevronDown } from "lucide-react"
import { useNavigate } from "react-router"

const GuildsSelector = () => {
  const { data: session } = useSession()
  const router = useNavigate()
  const { selectedGuild, setSelectedGuild } = useGuildsStore()
  const displayGuilds = session?.members.slice(0, 2)

  const handleSelectGuild = (guildId: number) => () => {
    setSelectedGuild(guildId)
    router(`/guilds/${guildId}`)
  }

  if (!session) {
    return null
  }

  return (
    <div className="bg-background dark:bg-input/30 flex h-9 items-center rounded-lg border shadow-xs">
      <div className="flex h-full items-center gap-2 px-1 py-1">
        {session?.members.length > 2 && selectedGuild && (
          <>
            <img className="size-7 rounded-lg" src={getCdnUrl(`guilds/${selectedGuild}/logo.jpg`)} />
            <Separator orientation="vertical" className="bg-input" />
          </>
        )}
        {displayGuilds?.map((guild) => (
          <img
            key={guild.guildId}
            onClick={handleSelectGuild(guild.guildId)}
            className="size-7 cursor-pointer rounded-lg"
            src={getCdnUrl(`guilds/${guild.guildId}/logo.jpg`)}
          />
        ))}
      </div>
      <Popover>
        <PopoverTrigger className="group" asChild>
          <button className="flex items-center gap-2 px-1 py-1">
            <ChevronDown className="text-muted-foreground size-5 transition group-data-[state=open]:rotate-180" />
          </button>
        </PopoverTrigger>
        <MenuGuildsSelector />
      </Popover>
    </div>
  )
}

export default GuildsSelector
