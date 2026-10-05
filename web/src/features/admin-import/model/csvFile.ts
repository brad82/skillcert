/** Spec §26: the browser reads the file and sends its text, up to 1 MB. */
export const maxCsvBytes = 1024 * 1024

export type CsvFile = { name: string; size: number; text: string }
export type FileProblem = 'tooBig' | 'notCsv' | null

export function checkFile(file: File): FileProblem {
  if (!/\.csv$/i.test(file.name) && file.type !== 'text/csv') return 'notCsv'
  if (file.size > maxCsvBytes) return 'tooBig'
  return null
}

/** A starter file with the documented header and two example rows (development plan §2.10). */
export const templateCsv = [
  'Code,Title,ShortTitle,Description,RecertificationDays,SelfReview,PeerReview,InstructorReview,SupervisorReview,Resources',
  '13.1,Avalanche transceiver search,Transceiver search,,365,,,Y,,"Search basics|https://example.org/t|Video"',
  '13.2,Probe line,,Organised probe line,730,,Y,,,',
].join('\n')
