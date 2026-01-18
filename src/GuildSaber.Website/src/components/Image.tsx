import { useCallback, useState, type ComponentProps } from "react"

interface Props extends ComponentProps<"img"> {
  banner?: boolean
}

const Image = ({ src, banner, ...props }: Props) => {
  const fallback = useCallback(() => {
    if (banner) {
      return "/images/fallback-banner.svg"
    }

    return "/images/fallback.svg"
  }, [banner])

  const [imgSrc, setImgSrc] = useState(src || fallback())

  const handleError = () => {
    setImgSrc(fallback())
  }

  return <img data-slot="image" src={imgSrc} onError={() => handleError()} {...props} />
}

export default Image
