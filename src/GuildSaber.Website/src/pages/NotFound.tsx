import ErrorState from "@/components/ErrorState"
import { Link2Off } from "lucide-react"

const NotFound = () => (
  <ErrorState
    icon={Link2Off}
    variant="info"
    title="Page not found"
    description="The page you're looking for doesn't exist or has been moved."
  />
)

export default NotFound
