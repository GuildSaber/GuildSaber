import Discord from "@/components/icons/Discord"
import { Button } from "@/components/ui/button"
import { generateAuthUrl } from "@/features/auth/utils"

const DiscordProvider = () => {
  const handleSignin = () => {
    window.location.href = generateAuthUrl("discord", "login")
  }

  return (
    <Button onClick={handleSignin} className="bg-[#5865F2] hover:bg-[#5865F2]/80">
      <Discord className="h-5 w-5 fill-white" />
      Login with Discord
    </Button>
  )
}

export default DiscordProvider
