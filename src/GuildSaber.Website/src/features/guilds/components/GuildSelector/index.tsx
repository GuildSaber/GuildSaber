import { Button } from "@/components/ui/button"
import { ButtonGroup } from "@/components/ui/button-group"
import { Popover, PopoverTrigger } from "@/components/ui/popover"
import { Separator } from "@/components/ui/separator"
import { useSession } from "@/features/auth/hooks/useSession"
import MenuGuildsSelector from "@/features/guilds/components/GuildSelector/MenuGuildsSelector"
import { useGuildsStore } from "@/features/guilds/stores/guildsStore"
import { getCdnUrl } from "@/utils/url"
import { ChevronDown } from "lucide-react"

const GuildsSelector = () => {
  const { data: session } = useSession()
  const { selectedGuild, setSelectedGuild } = useGuildsStore()
  const displayGuilds = session?.members.slice(0, 2)

  const handleSelectGuild = (guildId: number) => () => {
    if (guildId === selectedGuild) {
      setSelectedGuild(null)

      return
    }

    setSelectedGuild(guildId)
  }

  if (!session) {
    return null
  }

  return (
    <ButtonGroup>
      <div className="bg-background dark:bg-input/30 flex h-9 items-center gap-2 rounded-md border px-2 py-1 shadow-xs">
        {session?.members.length > 2 && selectedGuild && (
          <>
            <img className="size-7 rounded-lg" src={getCdnUrl(`guilds/${selectedGuild}/logo.jpg`)} />
            <Separator orientation="vertical" />
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
        <PopoverTrigger asChild>
          <Button variant="outline" className="px-2!">
            <ChevronDown />
          </Button>
        </PopoverTrigger>
        <MenuGuildsSelector />
      </Popover>
    </ButtonGroup>
  )
}

export default GuildsSelector
