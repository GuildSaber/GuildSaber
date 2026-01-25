import { Button } from "@/components/ui/button"
import { PopoverContent } from "@/components/ui/popover"
import { Separator } from "@/components/ui/separator"
import { useSession } from "@/features/auth/hooks/useSession"
import { useGuildsStore } from "@/features/guilds/stores/guildsStore"
import { getCdnUrl } from "@/utils/url"
import { Plus } from "lucide-react"
import { useNavigate } from "react-router"

const MenuGuildsSelector = () => {
  const { data: session } = useSession()
  const router = useNavigate()
  const { setSelectedGuild } = useGuildsStore()

  const handleSelectGuild = (guildId: number) => () => {
    setSelectedGuild(guildId)
    router(`/guilds/${guildId}`)
  }

  return (
    <PopoverContent sideOffset={10} align="end" className="w-auto min-w-30 p-0">
      {session?.members.map((guild) => (
        <Button
          key={guild.guildId}
          onClick={handleSelectGuild(guild.guildId)}
          variant="ghost"
          className="flex w-full items-center justify-start gap-2 px-2 py-5"
        >
          <img className="size-8 rounded-lg" src={getCdnUrl(`guilds/${guild.guildId}/logo.jpg`)} />
          <p>Challenge Saber</p>
        </Button>
      ))}

      <Separator />

      <Button variant="ghost" disabled className="flex w-full items-center justify-start gap-2 px-2 py-5">
        <div className="flex size-8 items-center justify-center rounded bg-black/20">
          <Plus className="size-5" />
        </div>
        <p>More Guilds</p>
      </Button>
    </PopoverContent>
  )
}

export default MenuGuildsSelector
