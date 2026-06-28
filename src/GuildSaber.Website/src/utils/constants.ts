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
  },
  Normal: {
    color: "#59b0f4",
    short: "N",
  },
  Hard: {
    color: "#ee5e44",
    short: "H",
  },
  Expert: {
    color: "#bf2a42",
    short: "Ex",
  },
  ExpertPlus: {
    color: "#8f48db",
    short: "Ex+",
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

export const MAP_MODIFIERS = [
  "NF",
  "IF",
  "BE",
  "DA",
  "FS",
  "SS",
  "SF",
  "GN",
  "NA",
  "NB",
  "NO",
  "PM",
  "SA",
  "SC",
] as const

export type MapModifier = (typeof MAP_MODIFIERS)[number]

export const MAP_SORT_BY = {
  Name: "Name",
  EditTime: "Edit Time",
  CreationTime: "Creation Time",
  DifficultyStar: "Difficulty",
  AccuracyStar: "Accuracy",
  Id: "ID",
}
