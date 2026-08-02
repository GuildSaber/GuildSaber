import Image from "@/components/Image"
import { useSession } from "@/features/auth/hooks/useSession"
import { useGuildsStore } from "@/features/guilds/stores/guildsStore"
import { getCdnUrl } from "@/utils/url"
import { useNavigate } from "react-router"

interface Props {
  onSelect?: () => void
}

const GuildsSelectorMobile = ({ onSelect }: Props) => {
  const { data: session } = useSession()
  const router = useNavigate()
  const { setSelectedGuild } = useGuildsStore()

  const handleSelectGuild = (guildId: number) => () => {
    setSelectedGuild(guildId)
    router(`/guilds/${guildId}`)
    onSelect?.()
  }

  if (!session) {
    return null
  }

  return (
    <div className="bg-background dark:bg-input/30 flex h-16 items-center gap-2 overflow-x-auto rounded-lg border px-2 shadow-xs">
      {session.members.map((guild) => (
        <Image
          key={guild.guildId}
          onClick={handleSelectGuild(guild.guildId)}
          className="size-12 shrink-0 cursor-pointer rounded-lg"
          src={getCdnUrl(`guilds/${guild.guildId}/logo.jpg`)}
        />
      ))}
    </div>
  )
}

export default GuildsSelectorMobile
