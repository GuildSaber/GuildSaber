import Discord from "@/components/icons/Discord"
import { Button } from "@/components/ui/button"
import { useSession } from "@/features/auth/hooks/useSession"
import { generateAuthUrl } from "@/features/auth/utils"
import { CircleCheck, Link } from "lucide-react"

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

  return (
    <div className="flex items-center gap-3">
      <div className="flex size-20 items-center justify-center rounded-lg bg-[#5865F2]">
        <Discord className="h-12 w-12 fill-white" />
      </div>
      <div>
        {discordId ? (
          <>
            <p className="flex items-center text-green-500">
              <CircleCheck className="mr-1 inline h-5 w-5" />
              Linked
            </p>
            <p className="mt-1 text-gray-200">
              Discord ID: <span className="rounded bg-gray-800 px-1 font-mono text-white">{discordId}</span>
            </p>
          </>
        ) : (
          <>
            <p className="mb-2">Link your Discord account</p>
            <Button onClick={handleLink}>
              <Link className="inline h-5 w-5" />
              Link
            </Button>
          </>
        )}
      </div>
    </div>
  )
}

export default DiscordLinkUser
