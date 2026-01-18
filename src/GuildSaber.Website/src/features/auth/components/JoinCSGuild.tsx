import { getPlayerExtendedAtMeOptions, joinGuildAtMeMutation } from "@/client/@tanstack/react-query.gen"
import { Button } from "@/components/ui/button"
import { useSession } from "@/features/auth/hooks/useSession"
import { useMutation, useQueryClient } from "@tanstack/react-query"
import { CircleCheck, CirclePlus } from "lucide-react"
import { Link } from "react-router"

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
  const isMember = members?.find((m) => m.guildId === 1)

  return (
    <div className="flex items-center gap-3">
      <img className="size-20 rounded-lg" src="https://cdn-dev.guildsaber.com/guilds/1/logo.jpg" />
      <div>
        <p className="text-2xl font-semibold">Challenge Saber</p>
        {isMember ? (
          <>
            <p className="flex items-center text-green-500">
              <CircleCheck className="mr-1 inline h-5 w-5" />
              Joined
            </p>
            <Link className="text-primary mb-2 underline" to="/guilds/1">
              View guild page
            </Link>
          </>
        ) : (
          <Button onClick={handleJoin}>
            <CirclePlus className="h-5 w-5" />
            Join
          </Button>
        )}
      </div>
    </div>
  )
}

export default JoinCSGuild
