import { useId } from "react"

const BeatLeaderColor = ({ className }: { className?: string }) => {
  const gradientId = useId()

  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width="16"
      height="16"
      className={className}
      viewBox="48.16 40.75 172.36 172.36"
    >
      <defs>
        <radialGradient id={gradientId} cx="184.24" cy="71.69" fx="156.97" r="205.3" gradientUnits="userSpaceOnUse">
          <stop offset="0.2" stopColor="#d90f41" />
          <stop offset="0.23" stopColor="#de0b54" />
          <stop offset="0.3" stopColor="#e4066d" />
          <stop offset="0.36" stopColor="#e9037e" />
          <stop offset="0.43" stopColor="#eb0189" />
          <stop offset="0.51" stopColor="#ec008c" />
          <stop offset="0.57" stopColor="#e9028d" />
          <stop offset="0.61" stopColor="#e10892" />
          <stop offset="0.64" stopColor="#d31399" />
          <stop offset="0.67" stopColor="#bf23a4" />
          <stop offset="0.69" stopColor="#a537b1" />
          <stop offset="0.71" stopColor="#8450c2" />
          <stop offset="0.74" stopColor="#5f6dd5" />
          <stop offset="0.75" stopColor="#4184e5" />
        </radialGradient>
      </defs>
      <circle cx="134.34" cy="126.93" r="27.15" className="fill-[#ec008e] dark:fill-white" />
      <path
        fill={`url(#${gradientId})`}
        fillRule="evenodd"
        d="M196.52,60.75a4,4,0,0,1,4,4V189.11a4,4,0,0,1-4,4H72.16a4,4,0,0,1-4-4V64.75a4,4,0,0,1,4-4ZM201.52,40.75H67.16a19,19,0,0,0-19,19V194.11a19,19,0,0,0,19,19H201.52a19,19,0,0,0,19-19V59.75a19,19,0,0,0-19-19Z"
      />
    </svg>
  )
}

export default BeatLeaderColor
