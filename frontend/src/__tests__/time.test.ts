import { describe, it, expect, beforeAll, afterAll, vi } from 'vitest'
import { timeAgo, formatDate } from '@/utils/time'

describe('timeAgo', () => {
  const BASE_NOW = new Date('2024-06-15T12:00:00Z')

  beforeAll(() => {
    // Fix "now" so tests are deterministic
    vi.useFakeTimers()
    vi.setSystemTime(BASE_NOW)
  })

  afterAll(() => {
    vi.useRealTimers()
  })

  it('returns "just now" for timestamps less than 10 seconds ago', () => {
    const fiveSecondsAgo = new Date(BASE_NOW.getTime() - 5_000).toISOString()
    expect(timeAgo(fiveSecondsAgo)).toBe('just now')
  })

  it('returns seconds for 10–59 seconds ago', () => {
    const thirtySecondsAgo = new Date(BASE_NOW.getTime() - 30_000).toISOString()
    expect(timeAgo(thirtySecondsAgo)).toBe('30 seconds ago')
  })

  it('returns "1 minute ago" for exactly 1 minute ago', () => {
    const oneMinuteAgo = new Date(BASE_NOW.getTime() - 60_000).toISOString()
    expect(timeAgo(oneMinuteAgo)).toBe('1 minute ago')
  })

  it('returns plural minutes for 2+ minutes ago', () => {
    const fiveMinutesAgo = new Date(BASE_NOW.getTime() - 5 * 60_000).toISOString()
    expect(timeAgo(fiveMinutesAgo)).toBe('5 minutes ago')
  })

  it('returns "1 hour ago" for exactly 1 hour ago', () => {
    const oneHourAgo = new Date(BASE_NOW.getTime() - 3_600_000).toISOString()
    expect(timeAgo(oneHourAgo)).toBe('1 hour ago')
  })

  it('returns plural hours for 2+ hours ago', () => {
    const threeHoursAgo = new Date(BASE_NOW.getTime() - 3 * 3_600_000).toISOString()
    expect(timeAgo(threeHoursAgo)).toBe('3 hours ago')
  })

  it('returns "yesterday" for exactly 1 day ago', () => {
    const oneDayAgo = new Date(BASE_NOW.getTime() - 24 * 3_600_000).toISOString()
    expect(timeAgo(oneDayAgo)).toBe('yesterday')
  })

  it('returns plural days for 2–6 days ago', () => {
    const threeDaysAgo = new Date(BASE_NOW.getTime() - 3 * 24 * 3_600_000).toISOString()
    expect(timeAgo(threeDaysAgo)).toBe('3 days ago')
  })

  it('returns "1 week ago" for exactly 1 week ago', () => {
    const oneWeekAgo = new Date(BASE_NOW.getTime() - 7 * 24 * 3_600_000).toISOString()
    expect(timeAgo(oneWeekAgo)).toBe('1 week ago')
  })

  it('returns plural weeks for 2–3 weeks ago', () => {
    const twoWeeksAgo = new Date(BASE_NOW.getTime() - 14 * 24 * 3_600_000).toISOString()
    expect(timeAgo(twoWeeksAgo)).toBe('2 weeks ago')
  })

  it('returns "1 month ago" for ~30 days ago', () => {
    const oneMonthAgo = new Date(BASE_NOW.getTime() - 30 * 24 * 3_600_000).toISOString()
    expect(timeAgo(oneMonthAgo)).toBe('1 month ago')
  })

  it('returns plural months for 2–11 months ago', () => {
    const sixMonthsAgo = new Date(BASE_NOW.getTime() - 6 * 30 * 24 * 3_600_000).toISOString()
    expect(timeAgo(sixMonthsAgo)).toBe('6 months ago')
  })

  it('returns "1 year ago" for ~365 days ago', () => {
    const oneYearAgo = new Date(BASE_NOW.getTime() - 365 * 24 * 3_600_000).toISOString()
    expect(timeAgo(oneYearAgo)).toBe('1 year ago')
  })

  it('returns plural years for 2+ years ago', () => {
    const twoYearsAgo = new Date(BASE_NOW.getTime() - 2 * 365 * 24 * 3_600_000).toISOString()
    expect(timeAgo(twoYearsAgo)).toBe('2 years ago')
  })
})

describe('formatDate', () => {
  it('returns a non-empty string for a valid ISO date', () => {
    const result = formatDate('2024-01-15T10:30:00Z')
    expect(result).toBeTruthy()
    expect(typeof result).toBe('string')
  })

  it('includes the year in the formatted output', () => {
    const result = formatDate('2024-06-15T12:00:00Z')
    expect(result).toContain('2024')
  })
})
