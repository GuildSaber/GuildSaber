import { getPlayerExtendedAtMeOptions, joinGuildAtMeMutation } from "@/client/@tanstack/react-query.gen"
import Image from "@/components/Image"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardFooter } from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { useSession } from "@/features/auth/hooks/useSession"
import { decimalToHex, getTextColor } from "@/utils/color"
import { getCdnUrl } from "@/utils/url"
import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"
import { useGuildContext } from "../contexts/guildContext"

const GuildHeader = () => {
  const queryClient = useQueryClient()
  const { data: session } = useSession()
  const guild = useGuildContext()

  const isMember = session?.members?.find((m) => m.guildId === guild?.guild.id) !== undefined

  const { mutate } = useMutation({
    ...joinGuildAtMeMutation(),
    onSuccess: () => {
      queryClient.invalidateQueries(getPlayerExtendedAtMeOptions())
    },
  })

  const handleJoin = () => {
    if (!guild) {
      return
    }

    mutate(
      {
        path: {
          guildId: guild.guild.id.toString(),
        },
      },
      { onSuccess: () => toast.success(`You joined ${guild.guild.info.name}`) },
    )
  }

  if (!guild) {
    return <LoadingSkeleton />
  }

  const guildColor = decimalToHex(guild?.guild.info.color)
  const colors = {
    text: getTextColor(guildColor),
    bg: guildColor,
  }

  return (
    <Card className="overflow-hidden p-0">
      {guild?.guild && (
        <Image
          className="z-0 h-20 w-full object-cover md:h-40"
          src={getCdnUrl(`guilds/${guild?.guild.id}/banner.jpg`)}
          banner={true}
        />
      )}
      <CardContent className="grid gap-6 md:grid-cols-[2fr_1fr]">
        <div>
          <div className="mb-2 flex flex-col items-center gap-4 md:flex-row md:items-start">
            <Image
              src={getCdnUrl(`guilds/${guild?.guild.id}/logo.jpg`)}
              alt={guild?.guild.info.name}
              className="border-card bg-card z-10 -mt-14 size-20 rounded-lg border-6 md:-mt-22 md:size-34"
            />

            <h1 className="line-clamp-2 text-xl font-bold md:text-3xl">{guild?.guild.info.name}</h1>
          </div>
          <p className="text-muted-foreground line-clamp-3 text-center md:text-left">{guild?.guild.info.description}</p>
        </div>

        <div className="flex items-end justify-between text-right md:flex-col md:justify-start">
          <div className="rounded p-0.5 px-1" style={{ backgroundColor: colors.bg }}>
            <p className="text-muted-foreground text-lg font-extrabold" style={{ color: colors.text }}>
              {guild?.guild.info.smallName}
            </p>
          </div>
          <Button onClick={handleJoin} disabled={isMember} className="md:mt-auto">
            {isMember ? "Joined" : "Join"}
          </Button>
        </div>
      </CardContent>
      <CardFooter />
    </Card>
  )
}

const LoadingSkeleton = () => (
  <Card className="overflow-hidden p-0">
    <Skeleton className="h-20 w-full rounded-none md:h-40" />
    <CardContent className="grid gap-6 md:grid-cols-[2fr_1fr]">
      <div>
        <div className="mb-2 flex flex-col items-center gap-4 md:flex-row md:items-start">
          <div className="border-card bg-card z-10 -mt-14 size-20 shrink-0 rounded-lg border-6 md:-mt-22 md:size-34">
            <Skeleton className="size-full rounded-lg" />
          </div>
          <Skeleton className="h-8 w-3/4 md:h-9" />
        </div>
        <Skeleton className="mx-auto h-4 w-2/3 md:mx-0" />
        <Skeleton className="mx-auto mt-2 h-4 w-1/2 md:mx-0" />
      </div>

      <div className="flex items-end justify-between text-right md:flex-col md:justify-start">
        <Skeleton className="h-8 w-16 rounded" />
        <Skeleton className="h-9 w-16 md:mt-auto" />
      </div>
    </CardContent>
    <CardFooter />
  </Card>
)

export default GuildHeader
