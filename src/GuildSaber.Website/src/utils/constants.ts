export const ORDER_BY = {
  Asc: "Ascending",
  Desc: "Descending",
}

export const MAX_STARS = 100

export const MIN_STARS = 0

export const MAX_BPM = 800

export const MIN_BPM = 0

export const MAP_DIFFICULTY = {
  Easy: {
    color: "#3cb371",
    short: "E",
    long: "Easy",
  },
  Normal: {
    color: "#59b0f4",
    short: "N",
    long: "Normal",
  },
  Hard: {
    color: "#ee5e44",
    short: "H",
    long: "Hard",
  },
  Expert: {
    color: "#bf2a42",
    short: "Ex",
    long: "Expert",
  },
  ExpertPlus: {
    color: "#8f48db",
    short: "Ex+",
    long: "Expert+",
  },
}

export const MAP_GAME_MODE: Record<string, string> = {
  Standard: "Standard",
  OneSaber: "One Saber",
  NoArrows: "No Arrows",
  "90Degree": "90°",
  "360Degree": "360°",
  Lightshow: "Lightshow",
  Lawless: "Lawless",
}

export const MAP_MODIFIERS = {
  NF: "NF",
  IF: "IF",
  BE: "BE",
  DA: "DA",
  FS: "FS",
  SS: "SS",
  SF: "SF",
  GN: "GN",
  NA: "NA",
  NB: "NB",
  NO: "NO",
  PM: "PM",
  SA: "SA",
  SC: "SC",
  OP: "OP",
} as const

export type MapModifier = keyof typeof MAP_MODIFIERS

export const PROHIBITED_DEFAULT_KEYS = [
  MAP_MODIFIERS.NO,
  MAP_MODIFIERS.NB,
  MAP_MODIFIERS.NF,
  MAP_MODIFIERS.SS,
  MAP_MODIFIERS.NA,
  MAP_MODIFIERS.OP,
] as const

export const MAP_SORT_BY = {
  Name: "Name",
  EditTime: "Edit Time",
  CreationTime: "Creation Time",
  DifficultyStar: "Difficulty",
  AccuracyStar: "Accuracy",
  Id: "ID",
}

export const LEADERBOARD_SORT_BY = {
  Points: "Points",
  EffectiveScore: "Score",
}

export const LEADERBOARD = {
  BeatLeader: "BeatLeader",
  ScoreSaber: "ScoreSaber",
} as const
