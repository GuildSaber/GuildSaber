import { parseAsInteger, useQueryState } from "nuqs"
import { useEffect, useRef } from "react"
import { useWindowSize } from "usehooks-ts"

interface RowSizeByDensity {
  normal: number
  compact: number
}

interface UseResponsivePageSizeOptions {
  page: number
  setPage: (page: number) => void
  isCompact: boolean
  columns: number
  rowHeight?: RowSizeByDensity
  minRows?: RowSizeByDensity
  reservedHeight?: number
  maxPageSize?: number
}

const DEFAULT_ROW_HEIGHT: RowSizeByDensity = { normal: 90, compact: 128 }
const DEFAULT_MIN_ROWS: RowSizeByDensity = { normal: 2, compact: 1 }
const DEFAULT_RESERVED_HEIGHT = 400
const DEFAULT_MAX_PAGE_SIZE = 100

export const useResponsivePageSize = ({
  page: currentPage,
  setPage,
  isCompact,
  columns,
  rowHeight = DEFAULT_ROW_HEIGHT,
  minRows = DEFAULT_MIN_ROWS,
  reservedHeight = DEFAULT_RESERVED_HEIGHT,
  maxPageSize = DEFAULT_MAX_PAGE_SIZE,
}: UseResponsivePageSizeOptions) => {
  const { height } = useWindowSize({ debounceDelay: 200 })
  const [urlPageSize, setUrlPageSize] = useQueryState("pageSize", parseAsInteger)

  const estimatedRowHeight = isCompact ? rowHeight.compact : rowHeight.normal
  const estimatedMinRows = isCompact ? minRows.compact : minRows.normal
  const availableHeight = Math.max(0, height - reservedHeight)

  const rowCount = Math.ceil(availableHeight / estimatedRowHeight)

  const pageSize = Math.min(maxPageSize, Math.max(estimatedMinRows, rowCount) * columns)

  const hasPageSize = urlPageSize !== null

  // Kept stable across our own pageSize corrections, otherwise cycling pageSize back and forth
  // drifts toward page 1 instead of returning to the original item.
  const anchorIndexRef = useRef(0)
  const lastCorrectedPageRef = useRef<number | null>(null)

  if (!hasPageSize) {
    anchorIndexRef.current = 0
  } else if (currentPage !== lastCorrectedPageRef.current) {
    anchorIndexRef.current = Math.max(0, currentPage - 1) * (urlPageSize ?? pageSize)
  }

  let page = 1

  if (hasPageSize) {
    page = urlPageSize === pageSize ? currentPage : Math.floor(anchorIndexRef.current / pageSize) + 1
  }

  const isInSync = hasPageSize && urlPageSize === pageSize && currentPage === page

  useEffect(() => {
    if (isInSync) {
      return
    }

    void setUrlPageSize(pageSize, { history: "replace" })

    if (currentPage !== page) {
      lastCorrectedPageRef.current = page
      setPage(page)
    }
  }, [isInSync, pageSize, page, currentPage, setPage, setUrlPageSize])

  return { page, pageSize }
}
