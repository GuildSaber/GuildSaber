"use client"

import {
  Pagination as PaginationComponent,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination"
import { cn } from "@/lib/utils"
import { Loader2 } from "lucide-react"
import { parseAsInteger, useQueryState } from "nuqs"
import { useMediaQuery } from "usehooks-ts"

interface Props {
  totalPages: number
  showEllipsis?: boolean
  isLoading?: boolean
  className?: string
}

const Pagination = ({ totalPages, showEllipsis = true, isLoading, className }: Props) => {
  const isMobile = useMediaQuery("(max-width: 639px)")
  const maxVisiblePages = isMobile ? 1 : 5
  const [currentPage, setCurrentPage] = useQueryState("page", parseAsInteger.withDefault(1))

  if (totalPages <= 1) {
    return null
  }

  const handlePageChange = (page: number) => {
    if (page >= 1 && page <= totalPages) {
      setCurrentPage(page)
    }
  }

  const getVisiblePages = () => {
    const adjustedMax = showEllipsis ? maxVisiblePages + 2 : maxVisiblePages

    if (totalPages <= adjustedMax) {
      return Array.from({ length: totalPages }, (_, i) => i + 1)
    }

    const halfVisible = Math.floor(maxVisiblePages / 2)
    let start = Math.max(currentPage - halfVisible, 1)
    let end = Math.min(start + maxVisiblePages - 1, totalPages)

    if (start <= 2) {
      start = 1
      end = Math.min(adjustedMax, totalPages)
    } else if (end >= totalPages - 1) {
      end = totalPages
      start = Math.max(totalPages - adjustedMax + 1, 1)
    }

    return Array.from({ length: end - start + 1 }, (_, i) => start + i)
  }

  const visiblePages = getVisiblePages()
  const showStartEllipsis = showEllipsis && visiblePages[0] > 1
  const showEndEllipsis = showEllipsis && visiblePages[visiblePages.length - 1] < totalPages

  return (
    <PaginationComponent className={cn("sticky bottom-6 mt-6 -ml-6 sm:ml-0 md:bottom-3", className)}>
      <PaginationContent className="bg-background rounded-lg border p-[0.2rem] select-none">
        <PaginationItem>
          <PaginationPrevious
            onClick={() => {
              handlePageChange(currentPage - 1)
            }}
            className={cn({
              "pointer-events-none opacity-50": currentPage === 1 || isLoading,
            })}
          />
        </PaginationItem>

        {showStartEllipsis && (
          <>
            <PaginationItem>
              <PaginationLink
                onClick={(e) => {
                  e.preventDefault()
                  handlePageChange(1)
                }}
                className={cn({
                  "pointer-events-none": isLoading,
                })}
              >
                1
              </PaginationLink>
            </PaginationItem>
            <PaginationItem>
              <PaginationEllipsis />
            </PaginationItem>
          </>
        )}

        {visiblePages.map((page) => (
          <PaginationItem key={page}>
            <PaginationLink
              onClick={() => {
                handlePageChange(page)
              }}
              isActive={currentPage === page}
              className={cn({
                "pointer-events-none": currentPage === page || isLoading,
              })}
            >
              {isLoading && currentPage === page ? <Loader2 className="animate-spin" /> : page}
            </PaginationLink>
          </PaginationItem>
        ))}

        {showEndEllipsis && (
          <>
            <PaginationItem>
              <PaginationEllipsis />
            </PaginationItem>
            <PaginationItem>
              <PaginationLink
                onClick={() => {
                  handlePageChange(totalPages)
                }}
                className={cn({
                  "pointer-events-none": isLoading,
                })}
              >
                {totalPages}
              </PaginationLink>
            </PaginationItem>
          </>
        )}

        <PaginationItem>
          <PaginationNext
            onClick={() => {
              handlePageChange(currentPage + 1)
            }}
            aria-disabled={currentPage === totalPages}
            className={cn({
              "pointer-events-none opacity-50": currentPage === totalPages || isLoading,
            })}
          />
        </PaginationItem>
      </PaginationContent>
    </PaginationComponent>
  )
}

export default Pagination
