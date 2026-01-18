import { colord, type AnyColor } from "colord"

/* eslint-disable no-bitwise */
export const decimalToHex = (argbInt?: number | string) => {
  if (!argbInt) {
    return "#000000"
  }

  const int = typeof argbInt === "string" ? parseInt(argbInt, 16) : argbInt
  const unsigned = (int & 0xffffffff) >>> 0
  const rgb = unsigned & 0x00ffffff

  return `#${rgb.toString(16).padStart(6, "0").toUpperCase()}`
}

export const decimalToRgba = (argbInt: number): [number, number, number, number] => {
  const unsigned = (argbInt & 0xffffffff) >>> 0

  const a = ((unsigned >> 24) & 0xff) / 255
  const r = (unsigned >> 16) & 0xff
  const g = (unsigned >> 8) & 0xff
  const b = unsigned & 0xff

  return [r, g, b, a]
}

export const getTextColor = (baseColor: AnyColor) => {
  const color = colord(baseColor)
  const hsl = color.toHsl()

  if (color.isDark()) {
    return colord({ h: hsl.h, s: hsl.s * 0.3, l: 90 }).toHex()
  }

  return colord({ h: hsl.h, s: hsl.s * 0.5, l: 20 }).toHex()
}
