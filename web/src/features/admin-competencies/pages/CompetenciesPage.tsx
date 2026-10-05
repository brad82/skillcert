import { Trans, useLingui } from '@lingui/react/macro'
import MenuItem from '@mui/material/MenuItem'
import Stack from '@mui/material/Stack'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { Upload } from 'lucide-react'
import { ButtonLink, TextLink } from '@shared/components/RouterLinks'
import { CompetenciesTable } from '../components/CompetenciesTable'
import type { CompetencyShow } from '../model/useCompetencyFilters'
import { useCompetencyFilterFields } from '../CompetenciesProvider'

/** All competencies: a secondary view for ones in no list, shared ones, inactive ones and fresh imports. */
export function CompetenciesPage() {
  const { search, setSearch, show, setShow } = useCompetencyFilterFields()
  const { t } = useLingui()
  return (
    <Stack spacing={6}>
      <Stack direction="row" spacing={4} useFlexGap sx={{ flexWrap: 'wrap', justifyContent: 'space-between', alignItems: 'flex-end' }}>
        <div>
          <Typography variant="h1"><Trans>All competencies</Trans></Typography>
          <Typography variant="body2" color="text.secondary">
            <Trans>Every competency across all lists, in code order. To edit a skill, it's usually quicker to start from its <TextLink to="/admin/lists">list</TextLink>.</Trans>
          </Typography>
        </div>
        <ButtonLink to="/admin/import" variant="outlined" startIcon={<Upload size={20} aria-hidden />}><Trans>Import from CSV</Trans></ButtonLink>
      </Stack>
      <Stack direction="row" spacing={4} useFlexGap sx={{ flexWrap: 'wrap' }}>
        <TextField label={t`Search by code or title`} type="search" value={search} onChange={(e) => setSearch(e.target.value)} sx={{ flex: '0 1 420px' }} />
        <TextField select label={t`Show`} value={show} onChange={(e) => setShow(e.target.value as CompetencyShow)} sx={{ minWidth: 240 }}>
          <MenuItem value="all">{t`All competencies`}</MenuItem>
          <MenuItem value="unlisted">{t`Not in any list`}</MenuItem>
          <MenuItem value="shared">{t`Shared by 2 or more lists`}</MenuItem>
          <MenuItem value="inactive">{t`Inactive`}</MenuItem>
        </TextField>
      </Stack>
      <CompetenciesTable />
    </Stack>
  )
}
