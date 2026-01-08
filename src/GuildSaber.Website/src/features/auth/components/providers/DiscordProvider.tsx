import Discord from "@/components/icons/Discord"
import { generateAuthUrl } from "@/features/auth/utils"

const DiscordProvider = () => {
  const handleSignin = () => {
    window.location.href = generateAuthUrl("discord", "login")
  }

  return (
    <button
      onClick={handleSignin}
      className="flex w-full cursor-pointer items-center justify-center gap-3 rounded-md bg-[#5865F2] fill-white p-2 text-white transition hover:opacity-80"
    >
      <Discord className="h-5 w-5" />
      <p>Login with Discord</p>
    </button>
  )
}

export default DiscordProvider
