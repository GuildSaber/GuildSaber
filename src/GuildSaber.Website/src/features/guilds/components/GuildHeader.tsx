import { getPlayerExtendedAtMeOptions, joinGuildAtMeMutation } from "@/client/@tanstack/react-query.gen"
import Image from "@/components/Image"
import { Button } from "@/components/ui/button"
import { Skeleton } from "@/components/ui/skeleton"
import { useSession } from "@/features/auth/hooks/useSession"
import { useGuildContext } from "@/features/guilds/contexts/guildContext"
import { decimalToHex, getTextColor } from "@/utils/color"
import { getCdnUrl } from "@/utils/url"
import { useMutation, useQueryClient } from "@tanstack/react-query"
import { toast } from "sonner"

const GuildHeader = () => {
  const queryClient = useQueryClient()
  const { data: session } = useSession()
  const guild = useGuildContext()

  const isMember = session?.members.find((m) => m.guildId === guild?.guild.id) !== undefined

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

  const guildColor = decimalToHex(guild.guild.info.color)
  const colors = {
    text: getTextColor(guildColor),
    bg: guildColor,
  }
  const guildBanner = getCdnUrl(`guilds/${guild.guild.id}/banner.jpg`)

  return (
    <div className="flex flex-col gap-4 overflow-hidden">
      {guildBanner && (
        <Image className="z-0 h-20 w-full rounded-xl object-cover md:h-40" src={guildBanner} banner={true} />
      )}
      <div className="grid gap-6 md:grid-cols-[2fr_1fr]">
        <div className="mb-2 flex flex-col items-center gap-4 md:flex-row md:items-start">
          <Image
            src={getCdnUrl(`guilds/${guild.guild.id}/logo.jpg`)}
            alt={guild.guild.info.name}
            className="bg-card border-background z-10 -mt-10 size-20 rounded-lg border-4 md:ml-4 md:size-34"
          />

          <div>
            <h1 className="line-clamp-2 text-center text-xl font-bold md:text-left md:text-3xl">
              {guild.guild.info.name}
            </h1>
            <p className="text-muted-foreground line-clamp-3 text-center md:text-left">
              {guild.guild.info.description}
            </p>
          </div>
        </div>

        <div className="flex items-end justify-between text-right md:flex-col md:justify-start">
          <div className="rounded p-0.5 px-1" style={{ backgroundColor: colors.bg }}>
            <p className="text-muted-foreground text-lg font-extrabold" style={{ color: colors.text }}>
              {guild.guild.info.smallName}
            </p>
          </div>
          <Button onClick={handleJoin} disabled={isMember} className="md:mt-auto">
            {isMember ? "Joined" : "Join"}
          </Button>
        </div>
      </div>
    </div>
  )
}

const LoadingSkeleton = () => (
  <div className="flex flex-col gap-4 overflow-hidden">
    <Skeleton className="h-20 w-full rounded-xl md:h-40" />
    <div className="grid gap-6 md:grid-cols-[2fr_1fr]">
      <div className="mb-2 flex flex-col items-center gap-4 md:flex-row md:items-start">
        <div className="bg-card z-10 -mt-10 size-20 shrink-0 rounded-lg md:size-34">
          <Skeleton className="size-full rounded-lg" />
        </div>
        <div className="w-full">
          <Skeleton className="h-8 w-3/4 md:h-9" />
          <Skeleton className="mx-auto mt-2 h-4 w-2/3 md:mx-0" />
          <Skeleton className="mx-auto mt-2 h-4 w-1/2 md:mx-0" />
        </div>
      </div>

      <div className="flex items-end justify-between text-right md:flex-col md:justify-start">
        <Skeleton className="h-8 w-16 rounded" />
        <Skeleton className="h-9 w-16 md:mt-auto" />
      </div>
    </div>
  </div>
)

export default GuildHeader
