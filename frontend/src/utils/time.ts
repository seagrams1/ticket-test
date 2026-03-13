/**
 * Returns a human-readable relative time string.
 * e.g. "2 hours ago", "just now", "3 days ago"
 */
export function timeAgo(isoString: string): string {
  const date = new Date(isoString)
  const now = new Date()
  const seconds = Math.floor((now.getTime() - date.getTime()) / 1000)

  if (seconds < 10) return 'just now'
  if (seconds < 60) return `${seconds} seconds ago`

  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return minutes === 1 ? '1 minute ago' : `${minutes} minutes ago`

  const hours = Math.floor(minutes / 60)
  if (hours < 24) return hours === 1 ? '1 hour ago' : `${hours} hours ago`

  const days = Math.floor(hours / 24)
  if (days < 7) return days === 1 ? 'yesterday' : `${days} days ago`

  const weeks = Math.floor(days / 7)
  if (weeks < 4) return weeks === 1 ? '1 week ago' : `${weeks} weeks ago`

  const months = Math.floor(days / 30)
  if (months < 12) return months === 1 ? '1 month ago' : `${months} months ago`

  const years = Math.floor(days / 365)
  return years === 1 ? '1 year ago' : `${years} years ago`
}

export function formatDate(isoString: string): string {
  return new Date(isoString).toLocaleString()
}
