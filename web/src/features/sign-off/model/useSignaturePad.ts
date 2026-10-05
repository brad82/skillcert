import { useCallback, useState } from 'react'
import type { SignatureRequest } from '@shared/api/model'

export type PadPoint = [x: number, y: number]

export type SignaturePad = {
  strokes: PadPoint[][]
  hasInk: boolean
  /** Pen down at a point in pad pixels. */
  start: (point: PadPoint) => void
  /** Pen moved while down. */
  extend: (point: PadPoint) => void
  clear: () => void
  /** The drawing as the API takes it; the server renders the SVG. */
  toRequest: (width: number, height: number) => SignatureRequest
}

const clamp = (value: number, max: number) => Math.min(Math.max(value, 0), max)
const round = (value: number) => Math.round(value * 10) / 10

/**
 * The strokes drawn on the signature pad (spec §10: vector strokes, rendered server-side). Owns only the points;
 * knows nothing about the canvas that draws them or when the signature is required.
 */
export function useSignaturePad(): SignaturePad {
  const [strokes, setStrokes] = useState<PadPoint[][]>([])
  const start = useCallback((point: PadPoint) => setStrokes((current) => [...current, [point]]), [])
  const extend = useCallback(
    (point: PadPoint) =>
      setStrokes((current) => (current.length === 0 ? current : [...current.slice(0, -1), [...current[current.length - 1]!, point]])),
    [],
  )
  const clear = useCallback(() => setStrokes([]), [])
  const toRequest = useCallback(
    (width: number, height: number): SignatureRequest => {
      const w = Math.max(1, Math.round(width))
      const h = Math.max(1, Math.round(height))
      return { width: w, height: h, strokes: strokes.map((stroke) => stroke.map(([x, y]) => [round(clamp(x, w)), round(clamp(y, h))])) }
    },
    [strokes],
  )
  return { strokes, hasInk: strokes.length > 0, start, extend, clear, toRequest }
}
