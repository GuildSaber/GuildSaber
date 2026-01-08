import Discord from "@/components/icons/Discord"
import { useSession } from "@/features/auth/hooks/useSession"
import { generateAuthUrl } from "@/features/auth/utils"

export const DiscordLinkUser = () => {
  const { data: session } = useSession()

  const handleLink = () => {
    window.location.href = generateAuthUrl("discord", "link")
  }

  if (!session) {
    return null
  }

  if (session.playerLinkedAccounts?.discordId) {
    return (
      <div className="text-center">
        <p>Your Discord account is already linked</p>
        <p>Discord ID: {session.playerLinkedAccounts.discordId}</p>
      </div>
    )
  }

  return (
    <button
      onClick={handleLink}
      className="flex w-full cursor-pointer items-center justify-center gap-3 rounded-md bg-[#5865F2] fill-white p-2 text-white transition hover:opacity-80"
    >
      <Discord className="h-5 w-5" />
      <p>Link with Discord</p>
    </button>
  )
}

export default DiscordLinkUser
