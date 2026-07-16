const isoToTwemoji = (iso: string) =>
  [...iso.toUpperCase()].map((c) => (0x1f1e6 + c.charCodeAt(0) - 65).toString(16)).join("-")

const getSvgFlag = (countryCode: string | undefined) => {
  if (!countryCode) {
    return ""
  }

  const filename = isoToTwemoji(countryCode)

  return `https://cdn.jsdelivr.net/gh/twitter/twemoji@14.0.2/assets/svg/${filename}.svg`
}

interface Props {
  code?: string
  className?: string
}

const Flag = ({ code, className }: Props) => {
  if (!code) {
    return null
  }

  return <img src={getSvgFlag(code)} alt={code} className={className} />
}

export default Flag
