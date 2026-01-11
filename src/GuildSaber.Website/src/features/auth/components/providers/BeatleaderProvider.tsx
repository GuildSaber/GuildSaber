import BeatLeader from "@/components/icons/Beatleader"
import { Button } from "@/components/ui/button"
import { generateAuthUrl } from "@/features/auth/utils"

const BeatleaderProvider = () => {
  const handleSignin = () => {
    window.location.href = generateAuthUrl("beatleader", "login")
  }

  return (
    <Button onClick={handleSignin} className="bg-[#c001c0] hover:bg-[#c001c0]/80">
      <BeatLeader className="h-5 w-5" />
      Signin with Beatleader
    </Button>
  )
}

export default BeatleaderProvider
