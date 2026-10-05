const collator = new Intl.Collator(undefined, { numeric: true })

/** Record order for codes: 9.1a before 11.1, 11.2 before 11.10. */
export const byCode = <T extends { code: string }>(items: readonly T[]): T[] => [...items].sort((a, b) => collator.compare(a.code, b.code))
