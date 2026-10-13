import { useId } from "react"

const ChroViewer = ({ className }: { className?: string }) => {
  const id = useId()

  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="16"
      height="16"
      className={className}
      viewBox="79 72 362 362"
      fill="none"
    >
      <defs>
        <linearGradient id={`${id}-red`} x1="106" y1="142" x2="256" y2="378" gradientUnits="userSpaceOnUse">
          <stop stopColor="#ff493d" />
          <stop offset="1" stopColor="#ff2d2d" />
        </linearGradient>
        <linearGradient id={`${id}-orange`} x1="155" y1="139" x2="279" y2="337" gradientUnits="userSpaceOnUse">
          <stop stopColor="#ff9b27" />
          <stop offset="1" stopColor="#ffb52d" />
        </linearGradient>
        <linearGradient id={`${id}-yellow`} x1="179" y1="151" x2="310" y2="347" gradientUnits="userSpaceOnUse">
          <stop stopColor="#ffd63d" />
          <stop offset="1" stopColor="#d3df45" />
        </linearGradient>
        <linearGradient id={`${id}-green`} x1="202" y1="164" x2="340" y2="356" gradientUnits="userSpaceOnUse">
          <stop stopColor="#55d9a5" />
          <stop offset="1" stopColor="#23c6b4" />
        </linearGradient>
        <linearGradient id={`${id}-cyan`} x1="225" y1="176" x2="371" y2="367" gradientUnits="userSpaceOnUse">
          <stop stopColor="#28c8d9" />
          <stop offset="1" stopColor="#168fe9" />
        </linearGradient>
        <linearGradient id={`${id}-blue`} x1="249" y1="188" x2="402" y2="377" gradientUnits="userSpaceOnUse">
          <stop stopColor="#20c8bd" />
          <stop offset="1" stopColor="#3538e8" />
        </linearGradient>
      </defs>
      <path
        d="M88 159L260 74Q264 72 264 78V247Q264 251 260 253L116 361L88 347Q84 345 84 341V165.44C84 162.7 85.55 160.21 88 159Z"
        fill={`url(#${id}-red)`}
        fillOpacity=".9"
      />
      <path
        d="M112 176C112 172 114 170 118 168L294.4 90.6Q298.4 88.6 298.4 94.6V266.4Q298.4 270.4 294.4 272.4L116 358Z"
        fill={`url(#${id}-orange)`}
        fillOpacity=".7"
      />
      <path
        d="M152 190L328.8 107.2Q332.8 105.2 332.8 111.2V285.8Q332.8 289.8 328.8 291.8L152 377Z"
        fill={`url(#${id}-yellow)`}
        fillOpacity=".7"
      />
      <path
        d="M188 208L363.2 123.8Q367.2 121.8 367.2 127.8V305.2Q367.2 309.2 363.2 311.2L188 396Z"
        fill={`url(#${id}-green)`}
        fillOpacity=".7"
      />
      <path
        d="M224 227L397.6 140.4Q401.6 138.4 401.6 144.4V324.6Q401.6 328.6 397.6 330.6L224 414Z"
        fill={`url(#${id}-cyan)`}
        fillOpacity=".7"
      />
      <path
        d="M259 242.1C261 242.1 263 240.5 266 239L432 157Q436 155 436 161V344Q436 348 432 350L266 433C264 434 262 434 260 433Z"
        fill={`url(#${id}-blue)`}
        fillOpacity=".78"
      />
      <path d="M155 240Q150 237 150 242V320Q150 325 154 327L256 384Q260 387 260 391V283Z" fill="#111319" />
      <path
        d="M116 171Q112 169 112 176V354Q112 359 116 361L260 433V391Q260 387 256 384L154 327Q150 325 150 320V242Q150 237 155 240L254 287Q260 290 260 283V246Q260 241 255 239Z"
        fill="white"
      />
    </svg>
  )
}

export default ChroViewer
