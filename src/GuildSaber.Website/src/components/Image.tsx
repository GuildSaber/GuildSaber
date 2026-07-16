import { useState, type ComponentProps } from "react"

interface Props extends ComponentProps<"img"> {
  banner?: boolean
}

const Image = ({ src, banner, ...props }: Props) => {
  const fallback = (() => {
    if (banner) {
      return "/images/fallback-banner.svg"
    }

    return "/images/fallback.svg"
  })()

  const [erroredSrc, setErroredSrc] = useState<typeof src>()

  const imgSrc = !src || src === erroredSrc ? fallback : src

  const handleError = () => setErroredSrc(src)

  return <img data-slot="image" src={imgSrc} onError={handleError} {...props} />
}

export default Image
