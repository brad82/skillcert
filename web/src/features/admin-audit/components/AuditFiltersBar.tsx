import { Trans, useLingui } from '@lingui/react/macro'
import Autocomplete from '@mui/material/Autocomplete'
import Button from '@mui/material/Button'
import Chip from '@mui/material/Chip'
import MenuItem from '@mui/material/MenuItem'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import { entityTypeLabels } from '../model/actionLabels'
import { useAuditFilters } from '../AuditProvider'

/** Type, who, date range, and the one item the log was opened for (from a user, list or competency). */
export function AuditFiltersBar() {
  const { filters, entityTypes, actors, entityLabel, set, clear } = useAuditFilters()
  const { t, i18n } = useLingui()
  const anyone = { id: '', name: t`Anyone` }
  const actorOptions = [anyone, ...actors]
  return (
    <Paper variant="outlined" sx={{ p: 4 }}>
      <Stack direction="row" spacing={3} useFlexGap sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
        <TextField select label={t`Type`} value={filters.entityType ?? ''} onChange={(e) => set({ entityType: e.target.value || undefined })} sx={{ minWidth: 200 }}>
          <MenuItem value="">{t`All types`}</MenuItem>
          {entityTypes.map((type) => (
            <MenuItem key={type} value={type}>{entityTypeLabels[type] ? i18n._(entityTypeLabels[type]) : type}</MenuItem>
          ))}
        </TextField>
        <Autocomplete
          options={actorOptions}
          getOptionLabel={(o) => o.name}
          value={actorOptions.find((o) => o.id === (filters.actorUserId ?? '')) ?? anyone}
          onChange={(_, option) => set({ actorUserId: option?.id || undefined })}
          isOptionEqualToValue={(a, b) => a.id === b.id}
          disableClearable
          renderInput={(params) => <TextField {...params} label={t`Who`} />}
          sx={{ minWidth: 220 }}
        />
        <TextField type="date" label={t`From`} value={filters.from ?? ''} onChange={(e) => set({ from: e.target.value || undefined })} slotProps={{ inputLabel: { shrink: true } }} />
        <TextField type="date" label={t`To`} value={filters.to ?? ''} onChange={(e) => set({ to: e.target.value || undefined })} slotProps={{ inputLabel: { shrink: true } }} />
        {filters.entityId && (
          <Chip label={entityLabel ? <Trans>Only: {entityLabel}</Trans> : <Trans>Only one item</Trans>} onDelete={() => set({ entityId: undefined })} />
        )}
        <Button onClick={clear}><Trans>Clear filters</Trans></Button>
      </Stack>
    </Paper>
  )
}
