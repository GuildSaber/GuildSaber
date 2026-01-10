import { getPlayerExtendedAtMeOptions, joinGuildAtMeMutation } from "@/client/@tanstack/react-query.gen"
import { useMutation, useQueryClient } from "@tanstack/react-query"
import { CircleCheck, CirclePlus } from "lucide-react"
import { useSession } from "../hooks/useSession"

const JoinCSGuild = () => {
  const queryClient = useQueryClient()
  const { data: session } = useSession()

  const { mutate } = useMutation({
    ...joinGuildAtMeMutation(),
    onSuccess: () => {
      queryClient.invalidateQueries(getPlayerExtendedAtMeOptions())
    },
  })

  const handleJoin = () =>
    mutate({
      path: {
        guildId: "1",
      },
    })

  if (!session) {
    return null
  }

  const { members } = session

  if (members?.find((m) => m.guildId === 1)) {
    return (
      <div className="mx-auto">
        <p className="flex items-center text-green-500">
          <CircleCheck className="mr-1 inline h-5 w-5" />
          You are join Challenge Saber guild
        </p>
      </div>
    )
  }

  return (
    <div className="flex flex-col items-center">
      <img
        className="mb-2 size-24 rounded-lg"
        src="https://images-ext-1.discordapp.net/external/bpNNtnJMXu66PiIWBMbSJ1BfYLeEHqX4xh7F933hdto/https/cdn-dev.guildsaber.com/guilds/1/logo.jpg?format=webp"
      />
      <button
        onClick={handleJoin}
        className="mx-auto flex cursor-pointer items-center justify-center gap-3 rounded-md bg-[#a6136f] fill-white p-2 text-white transition hover:opacity-80"
      >
        <CirclePlus className="h-5 w-5" />
        <p>Join Challenge Saber</p>
      </button>
    </div>
  )
}

export default JoinCSGuild
