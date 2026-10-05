import { useLingui } from '@lingui/react/macro'
import Box from '@mui/material/Box'
import type { Theme } from '@mui/material/styles'
import { type PointerEvent, type RefObject, useEffect, useRef } from 'react'
import type { PadPoint } from '../model/useSignaturePad'
import { useSignStep } from '../SignOffProvider'

type Props = {
  canvasRef: RefObject<HTMLCanvasElement | null>
}

const signaturePadStyles = () => ({
  canvas: (theme: Theme) => ({
    display: 'block',
    width: '100%',
    height: 180,
    bgcolor: '#fff', // paper-white in both modes: signatures are dark ink
    border: 1,
    borderColor: theme.palette.lineStrong,
    borderRadius: theme.radius.md,
    touchAction: 'none', // keep finger strokes from scrolling the page
    cursor: 'crosshair',
  }),
})

/** Draws the pad's strokes and turns pointer input (finger, pen, mouse) into points in CSS pixels. */
export function SignaturePadCanvas({ canvasRef }: Props) {
  const { pad } = useSignStep()
  const { t } = useLingui()
  const drawing = useRef(false)
  const styles = signaturePadStyles()

  useEffect(() => {
    const canvas = canvasRef.current
    const context = canvas?.getContext('2d')
    if (!canvas || !context) return
    const ratio = window.devicePixelRatio || 1
    canvas.width = Math.round(canvas.clientWidth * ratio)
    canvas.height = Math.round(canvas.clientHeight * ratio)
    context.setTransform(ratio, 0, 0, ratio, 0, 0)
    context.lineWidth = 3
    context.lineCap = 'round'
    context.lineJoin = 'round'
    context.strokeStyle = '#1f1a1b'
    for (const stroke of pad.strokes) {
      context.beginPath()
      context.moveTo(stroke[0]![0], stroke[0]![1])
      for (const [x, y] of stroke.length === 1 ? stroke : stroke.slice(1)) context.lineTo(x, y)
      context.stroke()
    }
  }, [canvasRef, pad.strokes])

  const pointAt = (event: PointerEvent<HTMLCanvasElement>): PadPoint => {
    const rect = event.currentTarget.getBoundingClientRect()
    return [event.clientX - rect.left, event.clientY - rect.top]
  }

  return (
    <Box
      component="canvas"
      ref={canvasRef}
      role="img"
      aria-label={t`Signature pad`}
      sx={styles.canvas}
      onPointerDown={(event: PointerEvent<HTMLCanvasElement>) => {
        drawing.current = true
        event.currentTarget.setPointerCapture?.(event.pointerId)
        pad.start(pointAt(event))
      }}
      onPointerMove={(event: PointerEvent<HTMLCanvasElement>) => {
        if (drawing.current) pad.extend(pointAt(event))
      }}
      onPointerUp={() => {
        drawing.current = false
      }}
      onPointerCancel={() => {
        drawing.current = false
      }}
    />
  )
}
