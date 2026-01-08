import BeatleaderProvider from "@/features/auth/components/providers/BeatleaderProvider"
import DiscordProvider from "@/features/auth/components/providers/DiscordProvider"

const SigninOptions = () => (
  <div className="flex flex-col gap-2">
    <BeatleaderProvider />
    <DiscordProvider />
  </div>
)

export default SigninOptions
