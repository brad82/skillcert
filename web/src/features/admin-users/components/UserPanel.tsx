import { Trans, useLingui } from '@lingui/react/macro'
import Button from '@mui/material/Button'
import Checkbox from '@mui/material/Checkbox'
import FormControlLabel from '@mui/material/FormControlLabel'
import FormGroup from '@mui/material/FormGroup'
import Paper from '@mui/material/Paper'
import Stack from '@mui/material/Stack'
import Typography from '@mui/material/Typography'
import { TextLink } from '@shared/components/RouterLinks'
import { formatDate } from '@shared/lib/dates'
import { reviewerLabel } from '@shared/lib/reviewers'
import { useLocale } from '@shared/i18n/AppI18nProvider'
import { useSelectedUser } from '../UsersProvider'

const userPanelStyles = () => ({
  panel: { p: 6, flex: { md: '0 0 340px' }, minWidth: 0 },
  facts: { display: 'grid', gridTemplateColumns: 'max-content 1fr', columnGap: 4, rowGap: 2, m: 0, '& dd': { m: 0 } },
  footer: { pt: 4, borderTop: 1, borderColor: 'divider' },
})

/** The selected user: facts, reviewer classifications (ticked on/off) and deactivate/reactivate. */
export function UserPanel() {
  const { user, classifications, isSelf, busy, toggleClassification, deactivate, reactivate } = useSelectedUser()
  const { locale } = useLocale()
  const { i18n } = useLingui()
  const styles = userPanelStyles()

  if (!user)
    return (
      <Paper variant="outlined" sx={styles.panel}>
        <Typography color="text.secondary"><Trans>Choose a user to see their details.</Trans></Typography>
      </Paper>
    )

  const firstName = user.displayName.split(' ')[0]
  return (
    <Paper variant="outlined" component="aside" aria-label={user.displayName} sx={styles.panel}>
      <Stack spacing={6}>
        <div>
          <Typography variant="h2">{user.displayName}</Typography>
          <Typography variant="body2" color="text.secondary">{user.email}</Typography>
        </div>
        <Typography component="dl" variant="body2" sx={styles.facts}>
          <Typography component="dt" variant="body2" color="text.secondary"><Trans>Status</Trans></Typography>
          <dd>{user.isActive ? <Trans>Active</Trans> : <Trans>Deactivated</Trans>}</dd>
          <Typography component="dt" variant="body2" color="text.secondary"><Trans>Administrator</Trans></Typography>
          <dd>{user.isAdministrator ? <Trans>Yes</Trans> : <Trans>No</Trans>}</dd>
          <Typography component="dt" variant="body2" color="text.secondary"><Trans>Groups</Trans></Typography>
          <dd>{user.groups.length > 0 ? user.groups.join(', ') : <Trans>None</Trans>}</dd>
          <Typography component="dt" variant="body2" color="text.secondary"><Trans>Created</Trans></Typography>
          <dd><Typography variant="date">{formatDate(user.createdAt, locale)}</Typography></dd>
        </Typography>
        <Stack component="fieldset" spacing={1} sx={{ border: 0, m: 0, p: 0 }}>
          <Typography component="legend" variant="h3" sx={{ mb: 2 }}><Trans>Reviewer classifications</Trans></Typography>
          <FormGroup>
            {classifications.map(({ code }) => (
              <FormControlLabel
                key={code}
                label={i18n._(reviewerLabel('Classified', code))}
                control={
                  <Checkbox
                    checked={user.classifications.includes(code)}
                    disabled={busy}
                    onChange={(event) => toggleClassification(code, event.target.checked)}
                  />
                }
              />
            ))}
          </FormGroup>
          <Typography variant="body2" color="text.secondary">
            <Trans>Removing a classification stops new reviews under it. {firstName} can still confirm or reject claims that already name them.</Trans>
          </Typography>
        </Stack>
        <Stack spacing={2} sx={styles.footer}>
          {isSelf ? (
            <Typography variant="body2" color="text.secondary"><Trans>You can't deactivate your own account.</Trans></Typography>
          ) : user.isActive ? (
            <>
              <Button variant="outlined" disabled={busy} onClick={deactivate} sx={{ alignSelf: 'flex-start' }}>
                <Trans>Deactivate {firstName}</Trans>
              </Button>
              <Typography variant="body2" color="text.secondary">
                <Trans>A deactivated user can't sign in or be chosen as a reviewer. Their history stays.</Trans>
              </Typography>
            </>
          ) : (
            <Button variant="outlined" disabled={busy} onClick={reactivate} sx={{ alignSelf: 'flex-start' }}>
              <Trans>Reactivate {firstName}</Trans>
            </Button>
          )}
          <TextLink to="/admin/audit" search={{ entityId: user.id }} variant="body2">
            <Trans>View this user's history</Trans>
          </TextLink>
        </Stack>
      </Stack>
    </Paper>
  )
}
