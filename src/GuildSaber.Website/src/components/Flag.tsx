const getSvgFlag = (countryCode: string | undefined) => {
  if (!countryCode) {
    return ""
  }

  return `https://cdn.jsdelivr.net/gh/lipis/flag-icons/flags/4x3/${countryCode.toLowerCase()}.svg`
}

type Props = {
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
