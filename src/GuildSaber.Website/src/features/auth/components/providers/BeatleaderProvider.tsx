import BeatLeader from "@/components/icons/Beatleader"
import { generateAuthUrl } from "@/features/auth/utils"

const BeatleaderProvider = () => {
  const handleSignin = () => {
    window.location.href = generateAuthUrl("beatleader", "login")
  }

  return (
    <button
      onClick={handleSignin}
      className="flex w-full cursor-pointer items-center justify-center gap-3 rounded-md bg-[#c001c0] p-2 text-white transition hover:opacity-80"
    >
      <BeatLeader className="h-5 w-5" />
      <p>Signin with Beatleader</p>
    </button>
  )
}

export default BeatleaderProvider
