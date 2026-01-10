import Discord from "@/components/icons/Discord"
import { useSession } from "@/features/auth/hooks/useSession"
import { generateAuthUrl } from "@/features/auth/utils"
import { CircleCheck } from "lucide-react"

export const DiscordLinkUser = () => {
  const { data: session } = useSession()

  const handleLink = () => {
    window.location.href = generateAuthUrl("discord", "link")
  }

  if (!session) {
    return null
  }

  const { player } = session
  const { discordId } = player.playerLinkedAccounts

  if (discordId) {
    return (
      <div className="mx-auto">
        <p className="flex items-center text-green-500">
          <CircleCheck className="mr-1 inline h-5 w-5" />
          Your Discord account is linked
        </p>
        <p className="mt-1 text-gray-200">
          Discord ID: <span className="rounded bg-slate-700 px-1 text-white">{discordId}</span>
        </p>
      </div>
    )
  }

  return (
    <button
      onClick={handleLink}
      className="mx-auto flex cursor-pointer items-center justify-center gap-3 rounded-md bg-[#5865F2] fill-white p-2 text-white transition hover:opacity-80"
    >
      <Discord className="h-5 w-5" />
      <p>Link with Discord</p>
    </button>
  )
}

export default DiscordLinkUser
