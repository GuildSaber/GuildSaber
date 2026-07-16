export const MAP_MODIFIER_NAMES = {
  NF: "No Fail",
  IF: "Insta Fail",
  BE: "Battery Energy",
  DA: "Disappearing Arrows",
  FS: "Faster Song",
  SS: "Slower Song",
  SF: "Super Fast Song",
  GN: "Ghost Notes",
  NA: "No Arrows",
  NB: "No Bombs",
  NO: "No Obstacles",
  PM: "Pro Mode",
  SA: "Strict Angles",
  SC: "Small Notes",
  OP: "Off Platform",
  OD: "Old Dots",
} as const

export type MapModifier = keyof typeof MAP_MODIFIER_NAMES

const normalizeModifierName = (name: string) => name.replace(/\s+/g, "").toLowerCase()

export const getModifierByShort = (short: string) => MAP_MODIFIER_NAMES[short as MapModifier] ?? short

export const getModifierByLong = (long: string) => {
  const normalized = normalizeModifierName(long)
  const short = Object.keys(MAP_MODIFIER_NAMES).find(
    (key) => normalizeModifierName(MAP_MODIFIER_NAMES[key as MapModifier]) === normalized,
  )

  return short ?? long
}

export const PROHIBITED_DEFAULT_KEYS = ["NO", "NB", "NF", "SS", "NA", "OP"] as const satisfies MapModifier[]
