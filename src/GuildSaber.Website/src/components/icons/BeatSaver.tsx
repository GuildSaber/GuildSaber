const BeatSaver = ({ className }: { className?: string }) => (
  <svg
    xmlns="http://www.w3.org/2000/svg"
    width="16"
    height="16"
    className={className}
    viewBox="0 0 200 200"
    fill="none"
    stroke="currentColor"
    strokeWidth="16"
  >
    <path d="M 100,7 189,47 100,87 12,47 Z" strokeLinejoin="round" />
    <path d="M 189,47 189,155 100,196 12,155 12,47" strokeLinejoin="round" />
    <path d="M 100,87 100,196" strokeLinejoin="round" />
    <path d="M 26,77 85,106 53,130 Z" strokeLinejoin="round" />
  </svg>
)

export default BeatSaver
