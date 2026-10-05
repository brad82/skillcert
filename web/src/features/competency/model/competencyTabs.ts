/** The detail screen's tabs (wireframe 3b), kept in the URL so back and refresh land on the same tab. */
export type CompetencyTab = 'overview' | 'resources' | 'history'

export const competencyTabs: readonly CompetencyTab[] = ['overview', 'resources', 'history']

export const isCompetencyTab = (value: unknown): value is CompetencyTab => competencyTabs.includes(value as CompetencyTab)
