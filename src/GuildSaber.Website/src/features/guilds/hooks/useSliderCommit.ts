import { useRef, useState } from "react"

export const useSliderCommit = (initialValue: number[], onCommit: (_v: number[]) => void) => {
  const [localValue, setLocalValue] = useState(initialValue)
  const latestRef = useRef(initialValue)

  const handleChange = (v: number[]) => {
    setLocalValue(v)
    latestRef.current = v
  }

  const handleCommit = () => {
    onCommit(latestRef.current)
  }

  const reset = (v: number[]) => {
    setLocalValue(v)
    latestRef.current = v
  }

  return { localValue, handleChange, handleCommit, reset }
}
